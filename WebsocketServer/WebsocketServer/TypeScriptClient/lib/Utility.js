var WebsocketTSClient;
(function (WebsocketTSClient) {
    var IsDebugEnv = true;
    var Utility = /** @class */ (function () {
        function Utility() {
        }
        Utility.GetCurrentUTC = function () {
            return new Date();
        };
        Utility.UTCNowMilliseconds = function () {
            return Date.now();
        };
        Utility.UTCNowSeconds = function () {
            return Math.floor(Date.now() / 1000);
        };
        /**
         * @param utcTimeString like: 03:40:20
         * @param dateTimeKind DateTimeKind.Local or DateTimeKind.Utc
         */
        Utility.ParseUTCTimeString = function (utcTimeString, dateTimeKind) {
            if (dateTimeKind === void 0) { dateTimeKind = DateTimeKind.Local; }
            if (!utcTimeString) {
                return null;
            }
            var segments = utcTimeString.split(':');
            if (!segments || segments.length !== 3) {
                return null;
            }
            var hourStr = segments[0], minsStr = segments[1], secsStr = segments[2];
            var hour = parseInt(hourStr, 10);
            var mins = parseInt(minsStr, 10);
            var secs = parseInt(secsStr, 10);
            if (isNaN(hour) || isNaN(mins) || isNaN(secs) ||
                hour < 0 || hour > 23 ||
                mins < 0 || mins > 59 ||
                secs < 0 || secs > 59) {
                return null;
            }
            // Date is meaningless, setting it to January 1, 2000
            return new Date(2000, 0, 1, hour, mins, secs);
        };
        Utility.LogDebug = function () {
            var data = [];
            for (var _i = 0; _i < arguments.length; _i++) {
                data[_i] = arguments[_i];
            }
            if (IsDebugEnv) {
                console.log(data);
            }
        };
        Utility.GenerateUniqueId = function () {
            // Get browser information
            var userAgent = "Browser";
            var random = Math.random().toString(36).substring(2, 10);
            var utc = Utility.UTCNowMilliseconds();
            var uniqueId = "".concat(userAgent, "-").concat(random, "-").concat(utc);
            return uniqueId;
        };
        return Utility;
    }());
    WebsocketTSClient.Utility = Utility;
    var DateTimeKind;
    (function (DateTimeKind) {
        DateTimeKind[DateTimeKind["Local"] = 0] = "Local";
        DateTimeKind[DateTimeKind["Utc"] = 1] = "Utc";
    })(DateTimeKind = WebsocketTSClient.DateTimeKind || (WebsocketTSClient.DateTimeKind = {}));
})(WebsocketTSClient || (WebsocketTSClient = {}));
//# sourceMappingURL=Utility.js.map