﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IndicatorsBasic
// Description: Collection of basic indicators
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Collection of basic indicators.
    /// </summary>
    public static class IndicatorsBasic
    {
        #region public static ITimeSeries<double> Lambda(Func<int, double> lambda)
        /// <summary>
        /// Create time series based on lambda, with lambda being executed once for
        /// every call to the indexer method. Use this for leight-weight lambdas.
        /// </summary>
        /// <param name="lambda">lambda, taking bars back as parameter and returning time series value</param>
        /// <param name="parentId">cache id used to identify functor</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>lambda time series</returns>
        public static ITimeSeries<double> Lambda(Func<int, double> lambda,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            // CAUTION:
            // lambda.GetHashCode() might not work w/ .Net Core
            // there are alternatives, see here:
            // https://stackoverflow.com/questions/283537/most-efficient-way-to-test-equality-of-lambda-expressions
            // however, we might not need to hash the lambda, as it is reasonably safe to assume
            // that for a different lambda, the call stack would also be different

            var cacheId = new CacheId(parentId, memberName, lineNumber,
                lambda.GetHashCode());

            var functor = Cache<FunctorLambda>.GetData(
                    cacheId,
                    () => new FunctorLambda(lambda));

            return functor;
        }

        private class FunctorLambda : ITimeSeries<double>
        {
            public readonly Func<int, double> Lambda;

            public FunctorLambda(Func<int, double> lambda)
            {
                Lambda = lambda;
            }

            public double this[int barsBack]
            {
                get
                {
                    return Lambda(barsBack);
                }
            }
        }
        #endregion
        #region public static ITimeSeries<double> BufferedLambda(Func<double, double> lambda, double first, params int[] identifier)
        /// <summary>
        /// Create time series based on lambda, with lambda being executed once for
        /// every new bar.
        /// </summary>
        /// <param name="lambda">lambda, with previous value as parameter and returning current time series value</param>
        /// <param name="first">first value to return</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>lambda time series</returns>
        public static ITimeSeries<double> BufferedLambda(Func<double, double> lambda, double first,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            // CAUTION:
            // lambda.GetHashCode() might not work w/ .Net Core
            // there are alternatives, see here:
            // https://stackoverflow.com/questions/283537/most-efficient-way-to-test-equality-of-lambda-expressions
            // however, we might not need to hash the lambda, as it is reasonably safe to assume
            // that for a different lambda, the call stack would also be different

            var cacheId = new CacheId(parentId, memberName, lineNumber,
                lambda.GetHashCode());

            var timeSeries = Cache<TimeSeries<double>>.GetData(
                cacheId,
                () => new TimeSeries<double>());

            double prevValue = timeSeries.BarsAvailable >= 1
                ? timeSeries[0]
                : first;

            timeSeries.Value = lambda(prevValue);

            return timeSeries;
        }
        #endregion

        #region public static ITimeSeries<double> Const(double constantValue)
        /// <summary>
        /// Return constant value time series.
        /// </summary>
        /// <param name="constantValue">value of time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>value as time series</returns>
        public static ITimeSeries<double> Const(double constantValue,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                constantValue.GetHashCode());

            return Lambda(
                (t) => constantValue,
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Delay(this ITimeSeries<double> series, int delay)
        /// <summary>
        /// Delay time series by number of bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="delay">delay length</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>delayed time series</returns>
        public static ITimeSeries<double> Delay(this ITimeSeries<double> series, int delay,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), delay);

            return Lambda(
                (t) => series[t + delay],
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate highest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>highest value of past n bars</returns>
        public static ITimeSeries<double> Highest(this ITimeSeries<double> series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            int N = Math.Max(1, n);

            return BufferedLambda(
                (v) => Enumerable.Range(0, N).Max(t => series[t]),
                series[0],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate lowest value of the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>lowest value of past n bars</returns>
        public static ITimeSeries<double> Lowest(this ITimeSeries<double> series, int n,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode(), n);

            int N = Math.Max(1, n);

            return BufferedLambda(
                (v) => Enumerable.Range(0, N).Min(t => series[t]),
                series[0],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Range(this Instrument series, int n)
        /// <summary>
        /// Calculate range over the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series (OHLC)</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>range between highest and lowest value of past n bars</returns>
        public static ITimeSeries<double> Range(this Instrument series, int n)
        {
            return series.High
                .Highest(n)
                .Subtract(series.Low
                    .Lowest(n));
        }
        #endregion
        #region public static ITimeSeries<double> Range(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Calculate range over the specified number of past bars.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">number of bars to search</param>
        /// <returns>range between highest and lowest value of past n bars</returns>
        public static ITimeSeries<double> Range(this ITimeSeries<double> series, int n)
        {
            return series
                .Highest(n)
                .Subtract(series
                    .Lowest(n));
        }
        #endregion
        #region public static ITimeSeries<double> Normalize(this ITimeSeries<double> series, int n)
        /// <summary>
        /// Normalize time series over number of bars; 1.0 being the average.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">normalizing period</param>
        /// <returns>normalized time series</returns>
        public static ITimeSeries<double> Normalize(this ITimeSeries<double> series, int n)
        {
            return series.Divide(series.EMA(n));
        }
        #endregion

        #region public static ITimeSeries<double> Return(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate absolute return, from the previous to the current
        /// value of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>absolute return</returns>
        public static ITimeSeries<double> Return(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return Lambda(
                (t) => series[t] - series[t + 1],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate logarithmic return from the previous to the current value
        /// of the time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>logarithm of relative return</returns>
        public static ITimeSeries<double> LogReturn(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return Lambda(
                (t) => Math.Log(series[t] / series[t + 1]),
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate absolute value of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>absolue value of input time series</returns>
        public static ITimeSeries<double> AbsValue(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Abs(series[t]),
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Square(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate square of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>squared input time series</returns>
        public static ITimeSeries<double> Square(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => series[t] * series[t],
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Sqrt(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate square root of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>square root time series</returns>
        public static ITimeSeries<double> Sqrt(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Sqrt(series[t]),
                cacheId);
        }
        #endregion
        #region public static ITimeSeries<double> Log(this ITimeSeries<double> series)
        /// <summary>
        /// Calculate natural logarithm of time series.
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>logarithmic time series</returns>
        public static ITimeSeries<double> Log(this ITimeSeries<double> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => Math.Log(series[t]),
                cacheId);
        }
        #endregion

        #region public static ITimeSeries<double> ToDouble(this ITimeSeries<long> series)
        /// <summary>
        /// Cast time series to double
        /// </summary>
        /// <param name="series">input series of long</param>
        /// <param name="parentId">caller cache id, optional</param>
        /// <param name="memberName">caller's member name, optional</param>
        /// <param name="lineNumber">caller line number, optional</param>
        /// <returns>output series of double</returns>
        public static ITimeSeries<double> ToDouble(this ITimeSeries<long> series,
            CacheId parentId = null, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            var cacheId = new CacheId(parentId, memberName, lineNumber,
                series.GetHashCode());

            return IndicatorsBasic.Lambda(
                (t) => (double)series[t],
                cacheId);
        }
        #endregion
    }
}

//==============================================================================
// end of file