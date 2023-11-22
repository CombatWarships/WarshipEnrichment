using System;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace WarshipEnrichment.Converters
{
    public static class RegExHelper
    {
        private static Regex _knotsRegex = new Regex("(\\d?\\d[;.]?\\d?\\d?) (knots|kn|kt)");
        private static Regex _tonsRegex = new Regex("(\\d+[,]?\\d+) (long tons|tons|tonnes|t|tn|tns)");
        private static Regex _inchesRegex = new Regex("(\\d?\\d?[;.]?\\d?\\d?)[ -](in|inch|inches|inch)");
        private static Regex _mmRegex = new Regex("(\\d?\\d?[;.]?\\d?\\d?) (mm|millimeters)");
        private static Regex _feetRegex = new Regex("(\\d?\\d?\\d?\\d[;.]?\\d?\\d?) (ft|feet)");
        private static Regex _shaftRegex = new Regex("(1|2|3|4)[x ×-]+(prop|propeller|screw propeller|screw|shaft)");
        private static Regex _yearRegex = new Regex("((18|19|20)\\d{2})");

        private static string _secondNumberSearch = "(\\d?\\d[;.]?\\d?\\d?)([–]|[-]| to ){0}";
        private static string _gunQuantityRegex = "(\\d+)[x ×]+(single|twin|triple)?[ ]?{0}";
        private static string _secondYearSearch = "[\\–|\\-| ]+((18|19|20)?\\d\\d)";
        private static string _thousandsRegex = "(\\d+[,]?\\d+)";
        private static string _secondThousandsRegex = "[\\-\\–](\\d+[,]?\\d+)";
        private static string _multiplierRegex = "(\\d+)[x ×]+";


        public static int? FindTons(string text)
        {
            return (int?)FindNumber(text, _tonsRegex);
        }

        public static double? FindKnots(string text)
        {
            return FindNumber(text, _knotsRegex);
        }

        public static double? FindFeet(string text)
        {
            return FindNumber(text, _feetRegex);
        }

        public static double? FindInches(string text)
        {
            var inches = FindNumber(text, _inchesRegex);
            if (inches != null)
                return inches;

            var mm = FindNumber(text, _mmRegex);
            if (mm != null)
                return mm / 25.4;

            return null;
        }

        internal static int? FindShafts(string text)
        {
            var shafts = (int?)FindNumber(text, _shaftRegex);
            if (shafts != null)
                return shafts;

            if (text.Contains("twin screws"))
                return 2;

            return null;
        }
        internal static int? FindGunQuantity(string text, int? caliber)
        {
            if (caliber == null)
                return null;

            var quantityRegex = string.Format(_gunQuantityRegex, caliber);

            if (string.IsNullOrEmpty(text))
                return null;

            text = text.ToLowerInvariant();

            var match = new Regex(quantityRegex).Match(text);
            if (match.Success)
            {

                var valueRaw = match.Groups[1].Value;
                if (!double.TryParse(valueRaw, out double value))
                    return null;

                var multiplier = match.Groups[2].Value;
                switch (multiplier)
                {
                    case "":
                    case "single":
                        break;
                    case "twin":
                        value *= 2;
                        break;
                    case "triple":
                        value *= 3;
                        break;
                }
                return (int?)value;
            }
            else
            {
                match = new Regex(_multiplierRegex).Match(text);
                if (match.Success)
                {
                    var valueRaw = match.Groups[1].Value;
                    if (!double.TryParse(valueRaw, out double value))
                        return null;
                    return (int?)value;
                }
            }
            return null;
        }

        public static void FindYear(string text, out int? firstYear, out int? secondYear)
        {
            secondYear = null;
            firstYear = null;

            var match = _yearRegex.Match(text);
            if (!match.Success)
                return;

            var value1Raw = match.Groups[1].Value;
            if (!double.TryParse(value1Raw, out double value))
                return;

            firstYear = (int?)value;

            // See if there is a range of numbers
            var search = firstYear + _secondYearSearch;
            Regex secondNumberRegex = new Regex(search);
            var match2 = secondNumberRegex.Match(text);

            if (match2.Success)
            {
                var valueRaw = match2.Groups[1].Value;
                if (double.TryParse(valueRaw, out double value2))
                    secondYear = (int?)value2;

                if (secondYear < 100)
                {
                    var baseYear = firstYear / 100;
                    baseYear *= 100;
                    secondYear += baseYear;
                }
            }
        }
        internal static void FindTonRange(string text, out int? smallestWeight, out int? largestWeight)
        {
            smallestWeight = null;
            largestWeight = null;

            var match = new Regex(_thousandsRegex).Match(text);
            if (!match.Success)
                return;

            var valueRaw = match.Groups[1].Value;
            if (!double.TryParse(valueRaw, out double value))
                return;
            smallestWeight = (int?)value;


            var secondThou = valueRaw + _secondThousandsRegex;
            match = new Regex(secondThou).Match(text);

            if (!match.Success)
                return;

            valueRaw = match.Groups[1].Value;
            if (!double.TryParse(valueRaw, out double value2))
                return;

            largestWeight = (int?)value2;
        }

        public static double? FindLargestInchFromRange(string text)
        {
            var match = _inchesRegex.Match(text);
            if (!match.Success)
                return FindInches(text);

            var value1Raw = match.Groups[1].Value;
            if (!double.TryParse(value1Raw, out double value))
            {
                return null;
            }

            // See if there is a range of numbers
            Regex secondNumberRegex = new Regex(string.Format(_secondNumberSearch, match.Groups[0]));
            var match2 = secondNumberRegex.Match(text);

            if (match2.Success)
            {
                var value2Raw = match2.Groups[1].Value;

                if (double.TryParse(value2Raw, out double value2))
                {
                    value = Math.Max(value, value2);
                }
            }

            //Console.WriteLine($"Captured: {value} FROM {text}");
            return value;
        }

        private static double? FindNumber(string text, Regex unitBasedSearch, int groupNumber = 1)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            text = text.ToLowerInvariant();

            var match = unitBasedSearch.Match(text);
            if (!match.Success)
                return null;

            var valueRaw = match.Groups[groupNumber].Value;
            if (!double.TryParse(valueRaw, out double value))
                return null;

            return value;
        }
    }
}
