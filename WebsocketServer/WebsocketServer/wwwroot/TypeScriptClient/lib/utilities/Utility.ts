namespace WebsocketTSClient
{
    const IsDebugEnv: boolean = true;

    export class Utility 
    {

        static GetCurrentUTC(): Date
        {
            return new Date();
        }

        static UTCNowMilliseconds(): number
        {
            return Date.now();
        }

        static UTCNowSeconds(): number
        {
            return Math.floor(Date.now() / 1000);
        }

        /**
         * @param utcTimeString like: 03:40:20
         * @param dateTimeKind DateTimeKind.Local or DateTimeKind.Utc
         */
        static ParseUTCTimeString(utcTimeString: string, dateTimeKind: DateTimeKind = DateTimeKind.Local): Date | null
        {
            if (!utcTimeString)
            {
                return null;
            }

            const segments: string[] = utcTimeString.split(':');
            if (!segments || segments.length !== 3)
            {
                return null;
            }

            const [hourStr, minsStr, secsStr] = segments;
            const hour: number = parseInt(hourStr, 10);
            const mins: number = parseInt(minsStr, 10);
            const secs: number = parseInt(secsStr, 10);

            if (isNaN(hour) || isNaN(mins) || isNaN(secs) ||
                hour < 0 || hour > 23 ||
                mins < 0 || mins > 59 ||
                secs < 0 || secs > 59)
            {
                return null;
            }

            // Date is meaningless, setting it to January 1, 2000
            return new Date(2000, 0, 1, hour, mins, secs);
        }

        static LogDebug(...data: any[]): void
        {
            if (IsDebugEnv && data.length > 0)
            {
                var str: string = "";
                data.forEach((d) => { str += d + " "});
                str.substring(0, str.length - 1);
                console.log(str);
            }
        }
        static GenerateUniqueId(): string
        {
            // Get browser information
            const userAgent = "Browser";
            const random = Math.random().toString(36).substring(2, 10);
            const utc = Utility.UTCNowMilliseconds();
            const uniqueId = `${userAgent}-${random}-${utc}`;
            return uniqueId;
        }

        static async delay(ms: number)
        {
            return new Promise(resolve => setTimeout(resolve, ms));
        }
    }

    export enum DateTimeKind
    {
        Local,
        Utc
    }
}