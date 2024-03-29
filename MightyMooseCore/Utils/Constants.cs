﻿namespace Eco.Moose.Utils.Constants
{
    public static class Constants
    {
        public static readonly string DEFAULT_CHAT_CHANNEL_NAME = "general";

        public const int SECONDS_PER_MINUTE = 60;
        public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * 60;
        public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * 24;
        public const int SECONDS_PER_WEEK = SECONDS_PER_DAY * 7;

        public const int MILLISECONDS_PER_MINUTE = SECONDS_PER_MINUTE * 1000;
        public const int MILLISECONDS_PER_HOUR = MILLISECONDS_PER_MINUTE * 60;
        public const int MILLISECONDS_PER_DAY = MILLISECONDS_PER_HOUR * 24;
        public const int MILLISECONDS_PER_WEEK = MILLISECONDS_PER_DAY * 7;

        public const int ECO_PLOT_SIZE_M2 = 5 * 5;
    }
}
