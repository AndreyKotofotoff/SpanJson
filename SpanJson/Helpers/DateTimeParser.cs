﻿using System;
using System.Diagnostics;

namespace SpanJson.Helpers
{
    /// <summary>
    /// Largely based on https://raw.githubusercontent.com/dotnet/corefx/f5d31619f821e7b4a0bcf7f648fe1dc2e4e2f09f/src/System.Memory/src/System/Buffers/Text/Utf8Parser/Utf8Parser.Date.O.cs
    /// Copyright (c) .NET Foundation and Contributors
    /// Modified to work for char and removed the 7 fractions requirement
    /// </summary>
    public static class DateTimeParser
    {

        public static bool TryParseDateTimeOffset(ReadOnlySpan<char> source, out DateTimeOffset value,
            out int charsConsumed)
        {
            return TryParseDateTimeOffsetIso8601(source, out value, out charsConsumed, out var kind);
        }

        public static bool TryParseDateTime(ReadOnlySpan<char> source, out DateTime value,
            out int charsConsumed)
        {
            if (TryParseDateTimeOffsetIso8601(source, out var dto, out charsConsumed, out var kind))
            {
                switch (kind)
                {
                    case DateTimeKind.Local:
                        value = dto.LocalDateTime;
                        break;
                    case DateTimeKind.Utc:
                        value = dto.UtcDateTime;
                        break;
                    default:
                        value = dto.DateTime;
                        break;

                }

                return true;
            }

            value = default;
            charsConsumed = 0;
            return false;
        }

        /// <summary>
        ///  2017-06-12T05:30:45.7680000-07:00
        ///  2017-06-12T05:30:45.7680000Z
        ///  2017-06-12T05:30:45.7680000 (local)
        ///  2017-06-12T05:30:45.768-07:00
        ///  2017-06-12T05:30:45.768Z
        ///  2017-06-12T05:30:45.768 (local)
        ///  2017-06-12T05:30:45
        ///  2017-06-12T05:30:45Z
        ///  2017-06-12T05:30:45 (local)
        /// </summary>
        private static bool TryParseDateTimeOffsetIso8601(ReadOnlySpan<char> source, out DateTimeOffset value,
            out int charsConsumed, out DateTimeKind kind)
        {

            if (source.Length < 19)
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int year;
            {
                var digit1 = source[0] - 48U; // '0' (this makes it uint)
                var digit2 = source[1] - 48U;
                var digit3 = source[2] - 48U;
                var digit4 = source[3] - 48U;

                if (digit1 > 9 || digit2 > 9 || digit3 > 9 || digit4 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                year = (int) (digit1 * 1000 + digit2 * 100 + digit3 * 10 + digit4);
            }

            if (source[4] != '-')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int month;
            {
                var digit1 = source[5] - 48U;
                var digit2 = source[6] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                month = (int) (digit1 * 10 + digit2);
            }

            if (source[7] != '-')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int day;
            {
                var digit1 = source[8] - 48U;
                var digit2 = source[9] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                day = (int) (digit1 * 10 + digit2);
            }

            if (source[10] != 'T')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int hour;
            {
                var digit1 = source[11] - 48U;
                var digit2 = source[12] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                hour = (int) (digit1 * 10 + digit2);
            }

            if (source[13] != ':')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int minute;
            {
                var digit1 = source[14] - 48U;
                var digit2 = source[15] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                minute = (int) (digit1 * 10 + digit2);
            }

            if (source[16] != ':')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int second;
            {
                var digit1 = source[17] - 48U;
                var digit2 = source[18] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                second = (int) (digit1 * 10 + digit2);
            }

            var currentOffset = 19; // up until here everything is fixed


            int fraction = 0;
            if (source.Length > currentOffset + 1 && source[currentOffset] == '.')
            {
                currentOffset++;
                var temp = source[currentOffset++] - 48U; // one needs to exist
                if (temp > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                var maxDigits = Math.Min(7, source.Length - currentOffset); // max 7 fraction digits
                for (var i = 0; i < maxDigits; i++)
                {
                    var digit = source[currentOffset] - 48U;
                    if (digit > 9)
                    {
                        break;
                    }

                    temp = temp * 10 + digit;
                    currentOffset++;
                }

                fraction = (int) temp;
            }

            var offsetChar = source.Length <= currentOffset ? default : source[currentOffset++];
            if (offsetChar != 'Z' && offsetChar != '+' && offsetChar != '-')
            {
                if (!TryCreateDateTime(year, month, day, hour, minute, second, fraction, DateTimeKind.Local,
                    out var dt))
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                value = dt;
                charsConsumed = currentOffset;
                kind = DateTimeKind.Unspecified;
                return true;
            }

            if (offsetChar == 'Z')
            {
                if (!TryCreateDateTimeOffset(year, month, day, hour, minute, second, fraction,
                    TimeSpan.Zero, out value))
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                charsConsumed = currentOffset;
                kind = DateTimeKind.Utc;
                return true;
            }

            Debug.Assert(offsetChar == '+' || offsetChar == '-');
            int offsetHours;
            {
                var digit1 = source[currentOffset++] - 48U;
                var digit2 = source[currentOffset++] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                offsetHours = (int) (digit1 * 10 + digit2);
            }

            if (source[currentOffset++] != ':')
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            int offsetMinutes;
            {
                var digit1 = source[currentOffset++] - 48U;
                var digit2 = source[currentOffset++] - 48U;

                if (digit1 > 9 || digit2 > 9)
                {
                    value = default;
                    charsConsumed = 0;
                    kind = default;
                    return false;
                }

                offsetMinutes = (int) (digit1 * 10 + digit2);
            }
            offsetHours = offsetChar == '-' ? -1 * offsetHours : offsetHours;
            if (!TryCreateDateTimeOffset(year, month, day, hour, minute, second, fraction,
                new TimeSpan(offsetHours, offsetMinutes, 0), out value))
            {
                value = default;
                charsConsumed = 0;
                kind = default;
                return false;
            }

            charsConsumed = currentOffset;
            kind = DateTimeKind.Local;
            return true;
        }

        private static bool TryCreateDateTime(int year, int month, int day, int hour, int minute, int second, int fraction, DateTimeKind kind, out DateTime value)
        {
            try
            {
                value = new DateTime(year, month, day, hour, minute, second, kind).AddTicks(fraction);
                return true;
            }
            catch (ArgumentException)
            {
            }

            value = default;
            return false;
        }

        private static bool TryCreateDateTimeOffset(int year, int month, int day, int hour, int minute, int second,
            int fraction, TimeSpan offset, out DateTimeOffset value)
        {
            try
            {
                value = new DateTimeOffset(year, month, day, hour, minute, second, offset).AddTicks(fraction);
                return true;
            }
            catch (ArgumentException)
            {
            }

            value = default;
            return false;
        }
    }
}