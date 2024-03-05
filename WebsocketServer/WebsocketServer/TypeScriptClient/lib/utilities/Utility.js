var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var WebsocketTSClient;
(function (WebsocketTSClient) {
    const IsDebugEnv = true;
    class Utility {
        static GetCurrentUTC() {
            return new Date();
        }
        static UTCNowMilliseconds() {
            return Date.now();
        }
        static UTCNowSeconds() {
            return Math.floor(Date.now() / 1000);
        }
        /**
         * @param utcTimeString like: 03:40:20
         * @param dateTimeKind DateTimeKind.Local or DateTimeKind.Utc
         */
        static ParseUTCTimeString(utcTimeString, dateTimeKind = DateTimeKind.Local) {
            if (!utcTimeString) {
                return null;
            }
            const segments = utcTimeString.split(':');
            if (!segments || segments.length !== 3) {
                return null;
            }
            const [hourStr, minsStr, secsStr] = segments;
            const hour = parseInt(hourStr, 10);
            const mins = parseInt(minsStr, 10);
            const secs = parseInt(secsStr, 10);
            if (isNaN(hour) || isNaN(mins) || isNaN(secs) ||
                hour < 0 || hour > 23 ||
                mins < 0 || mins > 59 ||
                secs < 0 || secs > 59) {
                return null;
            }
            // Date is meaningless, setting it to January 1, 2000
            return new Date(2000, 0, 1, hour, mins, secs);
        }
        static LogDebug(...data) {
            if (IsDebugEnv) {
                console.log(data);
            }
        }
        static GenerateUniqueId() {
            // Get browser information
            const userAgent = "Browser";
            const random = Math.random().toString(36).substring(2, 10);
            const utc = Utility.UTCNowMilliseconds();
            const uniqueId = `${userAgent}-${random}-${utc}`;
            return uniqueId;
        }
        static delay(ms) {
            return __awaiter(this, void 0, void 0, function* () {
                return new Promise(resolve => setTimeout(resolve, ms));
            });
        }
    }
    WebsocketTSClient.Utility = Utility;
    let DateTimeKind;
    (function (DateTimeKind) {
        DateTimeKind[DateTimeKind["Local"] = 0] = "Local";
        DateTimeKind[DateTimeKind["Utc"] = 1] = "Utc";
    })(DateTimeKind = WebsocketTSClient.DateTimeKind || (WebsocketTSClient.DateTimeKind = {}));
})(WebsocketTSClient || (WebsocketTSClient = {}));
