namespace WebsocketTSClient
{
    type EventParams = any[];
    export class EventHandler<T extends EventParams>
    {
        private event: ((...args: T) => void)[] = [];

        AddListener(handler: (...args: T) => void): void
        {
            this.event.push(handler);
        }

        RmListener(handler: (...args: T) => void): void
        {
            const index = this.event.indexOf(handler);
            if (index !== -1)
            {
                this.event.splice(index, 1);
            }
        }

        Trigger(...args: T): void
        {
            this.event.forEach(handler => handler(...args));
        }
    }
}