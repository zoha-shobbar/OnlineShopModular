
interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

class App {
    // For additional details, see the JsBridge.cs file.
    private static jsBridgeObj: DotNetObject;

    public static registerJsBridge(dotnetObj: DotNetObject) {
        App.jsBridgeObj = dotnetObj;
    }

    public static showDiagnostic() {
        return App.jsBridgeObj?.invokeMethodAsync('ShowDiagnostic');
    }

    public static publishMessage(message: string, payload: any) {
        return App.jsBridgeObj?.invokeMethodAsync('PublishMessage', message, payload);
    }

    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }

    public static openDevTools() {
        const allScripts = Array.from(document.scripts).map(s => s.src);
        const scriptAppended = allScripts.find(as => as.includes('npm/eruda'));

        if (scriptAppended) {
            (window as any).eruda.show();
            return;
        }

        const script = document.createElement('script');
        script.src = "https://cdn.jsdelivr.net/npm/eruda";
        document.body.append(script);
        script.onload = function () {
            (window as any).eruda.init();
            (window as any).eruda.show();
        }
    }

    public static async getPushNotificationSubscription(vapidPublicKey: string) {
        const registration = await navigator.serviceWorker.ready;
        if (!registration) return null;

        const pushManager = registration.pushManager;
        if (pushManager == null) return null;

        let subscription = await pushManager.getSubscription();

        if (subscription == null) {
            subscription = await pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: vapidPublicKey
            });
        }

        const pushChannel = subscription.toJSON();
        const p256dh = pushChannel.keys!['p256dh'];
        const auth = pushChannel.keys!['auth'];

        return {
            deviceId: `${p256dh}-${auth}`,
            platform: 'browser',
            p256dh: p256dh,
            auth: auth,
            endpoint: pushChannel.endpoint
        };
    };

    public static async forceUpdate() {
        const bswup = (window as any).BitBswup;
        const bswupProgress = (window as any).BitBswupProgress;

        if (await bswup.skipWaiting()) return;

        bswupProgress.config({ autoReload: true });
        bswup.checkForUpdate();
    }
}

(window as any).App = App;

window.addEventListener('message', handleMessage);
window.addEventListener('load', handleLoad);
window.addEventListener('resize', setCssWindowSizes);
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('message', handleMessage);
}

function handleMessage(e: MessageEvent) {
    // Enable publishing messages from JavaScript's `window.postMessage` or `client.postMessage` to the C# `PubSubService`.
    if (e.data.key === 'PUBLISH_MESSAGE') {
        App.publishMessage(e.data.message, e.data.payload);
    }
}

function handleLoad() {
    setCssWindowSizes();
}

function setCssWindowSizes() {
    document.documentElement.style.setProperty('--win-width', `${window.innerWidth}px`);
    document.documentElement.style.setProperty('--win-height', `${window.innerHeight}px`);
}

declare class BitTheme { static init(options: any): void; };

if (typeof BitTheme != "undefined") {
    BitTheme.init({
        system: true,
        persist: true,
        onChange: (newTheme: string, oldThem: string) => {
            if (newTheme === 'dark') {
                document.body.classList.add('theme-dark');
                document.body.classList.remove('theme-light');
            } else {
                document.body.classList.add('theme-light');
                document.body.classList.remove('theme-dark');
            }
            const primaryBgColor = getComputedStyle(document.documentElement).getPropertyValue('--bit-clr-bg-pri');
            document.querySelector('meta[name=theme-color]')!.setAttribute('content', primaryBgColor);
        }
    });
}
