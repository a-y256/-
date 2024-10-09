using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ExternalShortcutRadialMenu : MonoBehaviour
{
    // Windows APIのインポート
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // 定数の定義
    const uint WM_KEYDOWN = 0x0100;  // キーダウンイベント
    const uint WM_KEYUP = 0x0101;    // キーアップイベント
    const int GWL_EXSTYLE = -20;
    const uint WS_EX_LAYERED = 0x80000;  // ウィンドウを透過可能にする
    const uint LWA_COLORKEY = 0x1;       // 特定の色を透過するフラグ
    const uint SWP_NOMOVE = 0x0002;      // ウィンドウの位置を変更しない
    const uint SWP_NOSIZE = 0x0001;      // ウィンドウのサイズを変更しない
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);  // 最前面に設定
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);  // 最前面を解除

    // マウスイベント用の定数
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;

    // INPUT構造体の定義
    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // ショートカットのキー設定
    private ushort[][] shortcuts = new ushort[][]
    {
        new ushort[] { 0x11, 0x42 }, // Ctrl + B
        new ushort[] { 0x11, 0x43 }, // Ctrl + C
        new ushort[] { 0x11, 0x56 }, // Ctrl + V
        new ushort[] { 0x11, 0x5A }, // Ctrl + Z
        new ushort[] { 0x11, 0x6B }, // Ctrl + '+'
        new ushort[] { 0x11, 0xBD }, // Ctrl + '-'
        new ushort[] { 0x20 },       // Space
        new ushort[] { 0x2E }        // Delete
    };

    private string[] shortcutNames = new string[]
    {
        "Ctrl + B",
        "Ctrl + C",
        "Ctrl + V",
        "Ctrl + Z",
        "Ctrl + +",
        "Ctrl + -",
        "Space",
        "Delete"
    };

    public Text logText; // UnityのUI Textコンポーネントをアタッチ

    private void Update()
    {
        // Ctrl + Alt + Shift + Q が押された時、ビルドされたソフトを最前面に表示
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q))
        {
            IntPtr hWnd = GetForegroundWindow();
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            LogToUI("ビルドしたソフトが最前面になりました");
        }
    }

    // ボタンがクリックされた時にショートカットキーを送信する処理
    private void OnMenuButtonClicked(int index)
    {
        // "CapCut" ウィンドウを取得
        IntPtr hWndCapCut = FindWindow(null, "CapCut");

        if (hWndCapCut != IntPtr.Zero)
        {
            LogToUI("CapCutウィンドウが見つかりました");

            // 自分のウィンドウの最前面を解除
            IntPtr hWnd = GetForegroundWindow();
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            LogToUI("自分のウィンドウの最前面を解除");

            // CapCutのウィンドウを最前面に持ってくる
            SetForegroundWindow(hWndCapCut);
            BringWindowToTop(hWndCapCut);

            // マウスの左クリックを送信
            SimulateMouseClick();

            // 2秒待機してからショートカットを送信
            Thread.Sleep(500); // 2秒待機
            SimulateKeyPressHold(shortcuts[index]);  // ショートカットを送信

            // ショートカット名をログに表示
            LogToUI($"送信されたショートカット: {shortcutNames[index]}");

            // ショートカット送信後、さらに2秒待機してビルドしたソフトを再び最前面に戻す
            Thread.Sleep(500); // 2秒待機
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            LogToUI("ビルドしたソフトが再び最前面になりました");

            // 全てのキーを確実にリリースする
            ReleaseAllKeys();
        }
        else
        {
            LogToUI("CapCutウィンドウが見つかりませんでした");
        }
    }

    // 定数の追加
    const uint KEYEVENTF_KEYUP = 0x0002;  // キーを離すフラグ

    // キーイベント送信処理（SendInputを使用）
    private void SimulateKeyPressHold(ushort[] keyCodes)
    {
        int keyCount = keyCodes.Length;
        INPUT[] inputs = new INPUT[keyCount];

        // キーの押下
        for (int i = 0; i < keyCount; i++)
        {
            inputs[i].type = 1; // INPUT_KEYBOARD
            inputs[i].u.ki = new KEYBDINPUT { wVk = keyCodes[i], dwFlags = 0 };
        }

        // キーイベントの送信（キーを押す）
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

        // キーを離す
        ReleaseKeys(inputs);
    }

    // キーを離す処理
    private void ReleaseKeys(INPUT[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i].u.ki.dwFlags = KEYEVENTF_KEYUP; // キーを離すフラグを設定
        }

        // キーイベントの送信（キーを離す）
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // 全てのキーをリリースする処理（万が一他のキーが押されたままの状態になることを防ぐため）
    private void ReleaseAllKeys()
    {
        ushort[] modifierKeys = new ushort[] { 0x10, 0x11, 0x12 }; // Shift, Ctrl, Alt
        INPUT[] inputs = new INPUT[modifierKeys.Length];

        for (int i = 0; i < modifierKeys.Length; i++)
        {
            inputs[i].type = 1; // INPUT_KEYBOARD
            inputs[i].u.ki = new KEYBDINPUT { wVk = modifierKeys[i], dwFlags = KEYEVENTF_KEYUP };
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // マウスクリックをシミュレートするメソッド（現在のカーソル位置でクリック）
    private void SimulateMouseClick()
    {
        INPUT[] inputs = new INPUT[2];

        // マウス左ボタン押下
        inputs[0].type = 0; // INPUT_MOUSE
        inputs[0].u.mi = new MOUSEINPUT
        {
            dx = 0,
            dy = 0,
            mouseData = 0,
            dwFlags = MOUSEEVENTF_LEFTDOWN,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        // マウス左ボタン解放
        inputs[1].type = 0; // INPUT_MOUSE
        inputs[1].u.mi = new MOUSEINPUT
        {
            dx = 0,
            dy = 0,
            mouseData = 0,
            dwFlags = MOUSEEVENTF_LEFTUP,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        // マウスクリックイベントの送信
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    // ボタンがクリックされた時のイベント設定
    public Button[] menuButtons;

    private void Start()
    {
        // ウィンドウ透過処理
        IntPtr hWnd = GetForegroundWindow();
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);  // ウィンドウを透過可能に設定
        SetLayeredWindowAttributes(hWnd, 0x000000, 0, LWA_COLORKEY);  // 黒色部分を透過

        // **ここでウィンドウを最前面に設定**
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        LogToUI("アプリケーションが最前面に表示されました");

        // ボタンにイベントを追加
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            menuButtons[i].onClick.AddListener(() => OnMenuButtonClicked(index));
        }
    }

    // UIにログを表示するメソッド
    private void LogToUI(string message)
    {
        if (logText != null)
        {
            logText.text += "\n" + DateTime.Now.ToString("HH:mm:ss") + ": " + message;
        }
    }
}
