// Copyright QUANTOWER LLC. © 2017-2021. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;

namespace RangeFinder
{
    public class setLine
    {
        public int CandleIndex;
        public float xPrice;
        public float yPrice;
        public HistoryItemBar CandleBarBegin;
        public HistoryItemBar CandleBarEnd;

        public setLine(int candleIndex, float x, HistoryItemBar bar, HistoryItemBar bar2)
        {
            CandleIndex = candleIndex;
            xPrice = x;
            CandleBarBegin = bar;
            CandleBarEnd = bar2;
        }


    }

	public class RangeFinder : Indicator
    {
        List<double> PivotHighList = new List<double>();
        List<double> PivotLowList = new List<double>();
        public List<setLine> Lines = new List<setLine>();


        public RangeFinder()
            : base()
        {
            // Defines indicator's name and description.
            Name = "RangeFinder";
            Description = "Find the Range!";

            // Defines line on demand with particular parameters.
            AddLineSeries("PivotHighs", Color.Transparent, 1, LineStyle.Points).ShowLineMarker = false;
            AddLineSeries("PivotLows", Color.Transparent, 1, LineStyle.Points).ShowLineMarker = false;

            // By default indicator will be applied on main window of the chart
            SeparateWindow = false;
        }


        protected override void OnInit()
        {
            Lines = new List<setLine>();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            pivotHigh();
            pivotLow();



            isRange();
        }

        public double pivotHigh()
        {
            int startingIndex = Count - 6;
            List<double> highestHigh = new List<double>();
            HistoryItemBar candle = (HistoryItemBar)HistoricalData[startingIndex, SeekOriginHistory.Begin];

            for (int x = 1; x < 6; x++)
            {
                HistoryItemBar prevCandle = (HistoryItemBar)HistoricalData[startingIndex - x, SeekOriginHistory.Begin];
                HistoryItemBar forwardCandle = (HistoryItemBar)HistoricalData[startingIndex + x, SeekOriginHistory.Begin];

                highestHigh.Add(prevCandle.High);
                highestHigh.Add(forwardCandle.High);

            }

            double highestBack = highestHigh.Max();

            if (candle.High > highestBack)
            {
                SetValue(High(5), 0, 5);
                PivotHighList.Add(High(5));
                LinesSeries[0].SetMarker(5, new IndicatorLineMarker(Color.Purple, upperIcon: IndicatorLineMarkerIconType.DownArrow));
                return High(5);
            }
            else
            {
                SetValue(double.NaN);
                return double.NaN;
            }
        }

        public double pivotLow()
        {
            int startingIndex = Count - 6;

            List<double> lowestLow = new List<double>();
            HistoryItemBar candle = (HistoryItemBar)HistoricalData[startingIndex, SeekOriginHistory.Begin];

            for (int x = 1; x < 6; x++)
            {
                HistoryItemBar prevCandle = (HistoryItemBar)HistoricalData[startingIndex - x, SeekOriginHistory.Begin];
                HistoryItemBar forwardCandle = (HistoryItemBar)HistoricalData[startingIndex + x, SeekOriginHistory.Begin];

                lowestLow.Add(prevCandle.Low);
                lowestLow.Add(forwardCandle.Low);
            }

            double lowestBack = lowestLow.Min();
            //Core.Loggers.Log("Lowest COunt = " + candle.Low);


            if (candle.Low < lowestBack)
            {
                SetValue(Low(5), 1, 5);
                PivotLowList.Add(Low(5));
                LinesSeries[1].SetMarker(5, new IndicatorLineMarker(Color.Orange, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
                return Low(5);
            }
            else
            {
                SetValue(double.NaN);
                return double.NaN;
            }

        }

        public bool isRange()
        {
            List<double> lastTwoPivotsHigh = new List<double>();
            List<double> lastTwoPivotsLow = new List<double>();

            //High
            lastTwoPivotsHigh.Add(PivotHighList.Last());
            lastTwoPivotsHigh.Add(PivotHighList.AsEnumerable().Reverse().Skip(1).FirstOrDefault());

            //Low
            lastTwoPivotsLow.Add(PivotLowList.Last());
            lastTwoPivotsLow.Add(PivotLowList.AsEnumerable().Reverse().Skip(1).FirstOrDefault());

            int trackerHigh = 0;
            int trackerLow = 0;

            for (int x = 0; x < 10; x++)
            {
                HistoryItemBar candle = (HistoryItemBar)HistoricalData[Count - 1, SeekOriginHistory.Begin];
                HistoryItemBar beginCandle = (HistoryItemBar)HistoricalData[Count - 5, SeekOriginHistory.Begin];
                HistoryItemBar stopCandle = (HistoryItemBar)HistoricalData[Count + 5, SeekOriginHistory.Begin];

                //Highs
                double highLast = PivotHighList.Last();
                double secondLastHigh = PivotHighList.AsEnumerable().Reverse().Skip(1).FirstOrDefault();
                double highest = lastTwoPivotsHigh.Max();

                //Lows
                double lowLast = PivotLowList.Last();
                double secondLastLow = PivotLowList.AsEnumerable().Reverse().Skip(1).FirstOrDefault();
                double lowest = lastTwoPivotsLow.Min();

                

                //if two pivots are within 10 points of each other
                if (highLast > secondLastHigh && highLast < secondLastHigh + 10 || highLast < secondLastHigh && secondLastHigh < highLast + 10)
                {
                    int indexHigh = lastTwoPivotsHigh.IndexOf(lastTwoPivotsHigh.Max());
                    setLine topLine = new setLine(indexHigh, (float)highest, beginCandle, stopCandle);
                    Lines.Add(topLine);
                    trackerHigh = 1;

                    if (candle.Close < highest && candle.Close > lowest)
                    {
                        SetBarColor(Color.Purple);
                        return true;
                    }
                }

                if (lowLast < secondLastLow && lowLast > secondLastLow - 10 || lowLast > secondLastLow && secondLastLow > lowLast - 10)
                {
                    int indexLow = lastTwoPivotsLow.IndexOf(lastTwoPivotsLow.Min());
                    setLine botLine = new setLine(indexLow, (float)lowest, beginCandle, stopCandle);
                    Lines.Add(botLine);
                    trackerLow = 1;

                    if (candle.Close < highest && candle.Close > lowest)
                    {
                        SetBarColor(Color.Purple);
                        return true;
                    }
                }

                //if index of topLine
                if (trackerHigh == 1 && trackerLow == 1)
                {
                    if (candle.Close > highest || candle.Close < lowest)
                    {
                        trackerHigh = 0;
                        trackerLow = 0;
                        return false;
                    }

                    if (candle.Close > highest && trackerLow == 0)
                    {
                        return false;
                    }

                    if (candle.Close < lowest && trackerHigh == 0)
                    {
                        return false;
                    }

                }
            }
            

            return false;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            Graphics gr = args.Graphics;

            //Core.Loggers.Log("count = " + Lines.Count);
            Lines.FindAll(item => true).ForEach(item => {
                IChartWindow mainWindow = CurrentChart.MainWindow;

                int aXCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.CandleBarBegin.TimeLeft));
                int aYCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(item.xPrice));
                int bXCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.CandleBarEnd.TimeLeft));
                int bYCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(item.xPrice));

                // Draw a line using predefined Red pen
                gr.DrawLine(Pens.Red, aXCoord, aYCoord, bXCoord, bYCoord);
            });

            

        }

    }
}
