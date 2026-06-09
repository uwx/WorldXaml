namespace WorldXaml.UI.Yoga.Events;

public readonly struct Keys : IEquatable<Keys>, IComparable<Keys>
{
    /// <summary>
    /// Gets the state of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    public bool this[Key key] => GetKey((int)key);
    
    /// <summary>
    ///  No key pressed.
    /// </summary>
    public bool None => GetKey((int)Key.None);

    /// <summary>
    ///  The left mouse button.
    /// </summary>
    public bool LButton => GetKey((int)Key.LButton);

    /// <summary>
    ///  The right mouse button.
    /// </summary>
    public bool RButton => GetKey((int)Key.RButton);

    /// <summary>
    ///  The CANCEL key.
    /// </summary>
    public bool Cancel => GetKey((int)Key.Cancel);

    /// <summary>
    ///  The middle mouse button (three-button mouse).
    /// </summary>
    public bool MButton => GetKey((int)Key.MButton);

    /// <summary>
    ///  The first x mouse button (five-button mouse).
    /// </summary>
    public bool XButton1 => GetKey((int)Key.XButton1);

    /// <summary>
    ///  The second x mouse button (five-button mouse).
    /// </summary>
    public bool XButton2 => GetKey((int)Key.XButton2);

    /// <summary>
    ///  The BACKSPACE key.
    /// </summary>
    public bool Back => GetKey((int)Key.Back);

    /// <summary>
    ///  The TAB key.
    /// </summary>
    public bool Tab => GetKey((int)Key.Tab);

    /// <summary>
    ///  The CLEAR key.
    /// </summary>
    public bool LineFeed => GetKey((int)Key.LineFeed);

    /// <summary>
    ///  The CLEAR key.
    /// </summary>
    public bool Clear => GetKey((int)Key.Clear);

    /// <summary>
    ///  The RETURN key.
    /// </summary>
    public bool Return => GetKey((int)Key.Return);

    /// <summary>
    ///  The ENTER key.
    /// </summary>
    public bool Enter => GetKey((int)Key.Enter);

    /// <summary>
    ///  The SHIFT key.
    /// </summary>
    public bool ShiftKey => GetKey((int)Key.ShiftKey);

    /// <summary>
    ///  The CTRL key.
    /// </summary>
    public bool ControlKey => GetKey((int)Key.ControlKey);

    /// <summary>
    ///  The ALT key.
    /// </summary>
    public bool Menu => GetKey((int)Key.Menu);

    /// <summary>
    ///  The PAUSE key.
    /// </summary>
    public bool Pause => GetKey((int)Key.Pause);

    /// <summary>
    ///  The CAPS LOCK key.
    /// </summary>
    public bool Capital => GetKey((int)Key.Capital);

    /// <summary>
    ///  The CAPS LOCK key.
    /// </summary>
    public bool CapsLock => GetKey((int)Key.CapsLock);

    /// <summary>
    ///  The IME Kana mode key.
    /// </summary>
    public bool KanaMode => GetKey((int)Key.KanaMode);

    /// <summary>
    ///  The IME Hanguel mode key.
    /// </summary>
    public bool HanguelMode => GetKey((int)Key.HanguelMode);

    /// <summary>
    ///  The IME Hangul mode key.
    /// </summary>
    public bool HangulMode => GetKey((int)Key.HangulMode);

    /// <summary>
    ///  The IME Junja mode key.
    /// </summary>
    public bool JunjaMode => GetKey((int)Key.JunjaMode);

    /// <summary>
    ///  The IME Final mode key.
    /// </summary>
    public bool FinalMode => GetKey((int)Key.FinalMode);

    /// <summary>
    ///  The IME Hanja mode key.
    /// </summary>
    public bool HanjaMode => GetKey((int)Key.HanjaMode);

    /// <summary>
    ///  The IME Kanji mode key.
    /// </summary>
    public bool KanjiMode => GetKey((int)Key.KanjiMode);

    /// <summary>
    ///  The ESC key.
    /// </summary>
    public bool Escape => GetKey((int)Key.Escape);

    /// <summary>
    ///  The IME Convert key.
    /// </summary>
    public bool IMEConvert => GetKey((int)Key.IMEConvert);

    /// <summary>
    ///  The IME NonConvert key.
    /// </summary>
    public bool IMENonconvert => GetKey((int)Key.IMENonconvert);

    /// <summary>
    ///  The IME Accept key.
    /// </summary>
    public bool IMEAccept => GetKey((int)Key.IMEAccept);

    /// <summary>
    ///  The IME Accept key.
    /// </summary>
    public bool IMEAceept => GetKey((int)Key.IMEAceept);

    /// <summary>
    ///  The IME Mode change request.
    /// </summary>
    public bool IMEModeChange => GetKey((int)Key.IMEModeChange);

    /// <summary>
    ///  The SPACEBAR key.
    /// </summary>
    public bool Space => GetKey((int)Key.Space);

    /// <summary>
    ///  The PAGE UP key.
    /// </summary>
    public bool Prior => GetKey((int)Key.Prior);

    /// <summary>
    ///  The PAGE UP key.
    /// </summary>
    public bool PageUp => GetKey((int)Key.PageUp);

    /// <summary>
    ///  The PAGE DOWN key.
    /// </summary>
    public bool Next => GetKey((int)Key.Next);

    /// <summary>
    ///  The PAGE DOWN key.
    /// </summary>
    public bool PageDown => GetKey((int)Key.PageDown);

    /// <summary>
    ///  The END key.
    /// </summary>
    public bool End => GetKey((int)Key.End);

    /// <summary>
    ///  The HOME key.
    /// </summary>
    public bool Home => GetKey((int)Key.Home);

    /// <summary>
    ///  The LEFT ARROW key.
    /// </summary>
    public bool Left => GetKey((int)Key.Left);

    /// <summary>
    ///  The UP ARROW key.
    /// </summary>
    public bool Up => GetKey((int)Key.Up);

    /// <summary>
    ///  The RIGHT ARROW key.
    /// </summary>
    public bool Right => GetKey((int)Key.Right);

    /// <summary>
    ///  The DOWN ARROW key.
    /// </summary>
    public bool Down => GetKey((int)Key.Down);

    /// <summary>
    ///  The SELECT key.
    /// </summary>
    public bool Select => GetKey((int)Key.Select);

    /// <summary>
    ///  The PRINT key.
    /// </summary>
    public bool Print => GetKey((int)Key.Print);

    /// <summary>
    ///  The EXECUTE key.
    /// </summary>
    public bool Execute => GetKey((int)Key.Execute);

    /// <summary>
    ///  The PRINT SCREEN key.
    /// </summary>
    public bool Snapshot => GetKey((int)Key.Snapshot);

    /// <summary>
    ///  The PRINT SCREEN key.
    /// </summary>
    public bool PrintScreen => GetKey((int)Key.PrintScreen);

    /// <summary>
    ///  The INS key.
    /// </summary>
    public bool Insert => GetKey((int)Key.Insert);

    /// <summary>
    ///  The DEL key.
    /// </summary>
    public bool Delete => GetKey((int)Key.Delete);

    /// <summary>
    ///  The HELP key.
    /// </summary>
    public bool Help => GetKey((int)Key.Help);

    /// <summary>
    ///  The 0 key.
    /// </summary>
    public bool D0 => GetKey((int)Key.D0); // 0

    /// <summary>
    ///  The 1 key.
    /// </summary>
    public bool D1 => GetKey((int)Key.D1); // 1

    /// <summary>
    ///  The 2 key.
    /// </summary>
    public bool D2 => GetKey((int)Key.D2); // 2

    /// <summary>
    ///  The 3 key.
    /// </summary>
    public bool D3 => GetKey((int)Key.D3); // 3

    /// <summary>
    ///  The 4 key.
    /// </summary>
    public bool D4 => GetKey((int)Key.D4); // 4

    /// <summary>
    ///  The 5 key.
    /// </summary>
    public bool D5 => GetKey((int)Key.D5); // 5

    /// <summary>
    ///  The 6 key.
    /// </summary>
    public bool D6 => GetKey((int)Key.D6); // 6

    /// <summary>
    ///  The 7 key.
    /// </summary>
    public bool D7 => GetKey((int)Key.D7); // 7

    /// <summary>
    ///  The 8 key.
    /// </summary>
    public bool D8 => GetKey((int)Key.D8); // 8

    /// <summary>
    ///  The 9 key.
    /// </summary>
    public bool D9 => GetKey((int)Key.D9); // 9

    /// <summary>
    ///  The A key.
    /// </summary>
    public bool A => GetKey((int)Key.A);

    /// <summary>
    ///  The B key.
    /// </summary>
    public bool B => GetKey((int)Key.B);

    /// <summary>
    ///  The C key.
    /// </summary>
    public bool C => GetKey((int)Key.C);

    /// <summary>
    ///  The D key.
    /// </summary>
    public bool D => GetKey((int)Key.D);

    /// <summary>
    ///  The E key.
    /// </summary>
    public bool E => GetKey((int)Key.E);

    /// <summary>
    ///  The F key.
    /// </summary>
    public bool F => GetKey((int)Key.F);

    /// <summary>
    ///  The G key.
    /// </summary>
    public bool G => GetKey((int)Key.G);

    /// <summary>
    ///  The H key.
    /// </summary>
    public bool H => GetKey((int)Key.H);

    /// <summary>
    ///  The I key.
    /// </summary>
    public bool I => GetKey((int)Key.I);

    /// <summary>
    ///  The J key.
    /// </summary>
    public bool J => GetKey((int)Key.J);

    /// <summary>
    ///  The K key.
    /// </summary>
    public bool K => GetKey((int)Key.K);

    /// <summary>
    ///  The L key.
    /// </summary>
    public bool L => GetKey((int)Key.L);

    /// <summary>
    ///  The M key.
    /// </summary>
    public bool M => GetKey((int)Key.M);

    /// <summary>
    ///  The N key.
    /// </summary>
    public bool N => GetKey((int)Key.N);

    /// <summary>
    ///  The O key.
    /// </summary>
    public bool O => GetKey((int)Key.O);

    /// <summary>
    ///  The P key.
    /// </summary>
    public bool P => GetKey((int)Key.P);

    /// <summary>
    ///  The Q key.
    /// </summary>
    public bool Q => GetKey((int)Key.Q);

    /// <summary>
    ///  The R key.
    /// </summary>
    public bool R => GetKey((int)Key.R);

    /// <summary>
    ///  The S key.
    /// </summary>
    public bool S => GetKey((int)Key.S);

    /// <summary>
    ///  The T key.
    /// </summary>
    public bool T => GetKey((int)Key.T);

    /// <summary>
    ///  The U key.
    /// </summary>
    public bool U => GetKey((int)Key.U);

    /// <summary>
    ///  The V key.
    /// </summary>
    public bool V => GetKey((int)Key.V);

    /// <summary>
    ///  The W key.
    /// </summary>
    public bool W => GetKey((int)Key.W);

    /// <summary>
    ///  The X key.
    /// </summary>
    public bool X => GetKey((int)Key.X);

    /// <summary>
    ///  The Y key.
    /// </summary>
    public bool Y => GetKey((int)Key.Y);

    /// <summary>
    ///  The Z key.
    /// </summary>
    public bool Z => GetKey((int)Key.Z);

    /// <summary>
    ///  The left Windows logo key (Microsoft Natural Keyboard).
    /// </summary>
    public bool LWin => GetKey((int)Key.LWin);

    /// <summary>
    ///  The right Windows logo key (Microsoft Natural Keyboard).
    /// </summary>
    public bool RWin => GetKey((int)Key.RWin);

    /// <summary>
    ///  The Application key (Microsoft Natural Keyboard).
    /// </summary>
    public bool Apps => GetKey((int)Key.Apps);

    /// <summary>
    ///  The Computer Sleep key.
    /// </summary>
    public bool Sleep => GetKey((int)Key.Sleep);

    /// <summary>
    ///  The 0 key on the numeric keypad.
    /// </summary>
    public bool NumPad0 => GetKey((int)Key.NumPad0);

    /// <summary>
    ///  The 1 key on the numeric keypad.
    /// </summary>
    public bool NumPad1 => GetKey((int)Key.NumPad1);

    /// <summary>
    ///  The 2 key on the numeric keypad.
    /// </summary>
    public bool NumPad2 => GetKey((int)Key.NumPad2);

    /// <summary>
    ///  The 3 key on the numeric keypad.
    /// </summary>
    public bool NumPad3 => GetKey((int)Key.NumPad3);

    /// <summary>
    ///  The 4 key on the numeric keypad.
    /// </summary>
    public bool NumPad4 => GetKey((int)Key.NumPad4);

    /// <summary>
    ///  The 5 key on the numeric keypad.
    /// </summary>
    public bool NumPad5 => GetKey((int)Key.NumPad5);

    /// <summary>
    ///  The 6 key on the numeric keypad.
    /// </summary>
    public bool NumPad6 => GetKey((int)Key.NumPad6);

    /// <summary>
    ///  The 7 key on the numeric keypad.
    /// </summary>
    public bool NumPad7 => GetKey((int)Key.NumPad7);

    /// <summary>
    ///  The 8 key on the numeric keypad.
    /// </summary>
    public bool NumPad8 => GetKey((int)Key.NumPad8);

    /// <summary>
    ///  The 9 key on the numeric keypad.
    /// </summary>
    public bool NumPad9 => GetKey((int)Key.NumPad9);

    /// <summary>
    ///  The Multiply key.
    /// </summary>
    public bool Multiply => GetKey((int)Key.Multiply);

    /// <summary>
    ///  The Add key.
    /// </summary>
    public bool Add => GetKey((int)Key.Add);

    /// <summary>
    ///  The Separator key.
    /// </summary>
    public bool Separator => GetKey((int)Key.Separator);

    /// <summary>
    ///  The Subtract key.
    /// </summary>
    public bool Subtract => GetKey((int)Key.Subtract);

    /// <summary>
    ///  The Decimal key.
    /// </summary>
    public bool Decimal => GetKey((int)Key.Decimal);

    /// <summary>
    ///  The Divide key.
    /// </summary>
    public bool Divide => GetKey((int)Key.Divide);

    /// <summary>
    ///  The F1 key.
    /// </summary>
    public bool F1 => GetKey((int)Key.F1);

    /// <summary>
    ///  The F2 key.
    /// </summary>
    public bool F2 => GetKey((int)Key.F2);

    /// <summary>
    ///  The F3 key.
    /// </summary>
    public bool F3 => GetKey((int)Key.F3);

    /// <summary>
    ///  The F4 key.
    /// </summary>
    public bool F4 => GetKey((int)Key.F4);

    /// <summary>
    ///  The F5 key.
    /// </summary>
    public bool F5 => GetKey((int)Key.F5);

    /// <summary>
    ///  The F6 key.
    /// </summary>
    public bool F6 => GetKey((int)Key.F6);

    /// <summary>
    ///  The F7 key.
    /// </summary>
    public bool F7 => GetKey((int)Key.F7);

    /// <summary>
    ///  The F8 key.
    /// </summary>
    public bool F8 => GetKey((int)Key.F8);

    /// <summary>
    ///  The F9 key.
    /// </summary>
    public bool F9 => GetKey((int)Key.F9);

    /// <summary>
    ///  The F10 key.
    /// </summary>
    public bool F10 => GetKey((int)Key.F10);

    /// <summary>
    ///  The F11 key.
    /// </summary>
    public bool F11 => GetKey((int)Key.F11);

    /// <summary>
    ///  The F12 key.
    /// </summary>
    public bool F12 => GetKey((int)Key.F12);

    /// <summary>
    ///  The F13 key.
    /// </summary>
    public bool F13 => GetKey((int)Key.F13);

    /// <summary>
    ///  The F14 key.
    /// </summary>
    public bool F14 => GetKey((int)Key.F14);

    /// <summary>
    ///  The F15 key.
    /// </summary>
    public bool F15 => GetKey((int)Key.F15);

    /// <summary>
    ///  The F16 key.
    /// </summary>
    public bool F16 => GetKey((int)Key.F16);

    /// <summary>
    ///  The F17 key.
    /// </summary>
    public bool F17 => GetKey((int)Key.F17);

    /// <summary>
    ///  The F18 key.
    /// </summary>
    public bool F18 => GetKey((int)Key.F18);

    /// <summary>
    ///  The F19 key.
    /// </summary>
    public bool F19 => GetKey((int)Key.F19);

    /// <summary>
    ///  The F20 key.
    /// </summary>
    public bool F20 => GetKey((int)Key.F20);

    /// <summary>
    ///  The F21 key.
    /// </summary>
    public bool F21 => GetKey((int)Key.F21);

    /// <summary>
    ///  The F22 key.
    /// </summary>
    public bool F22 => GetKey((int)Key.F22);

    /// <summary>
    ///  The F23 key.
    /// </summary>
    public bool F23 => GetKey((int)Key.F23);

    /// <summary>
    ///  The F24 key.
    /// </summary>
    public bool F24 => GetKey((int)Key.F24);

    /// <summary>
    ///  The NUM LOCK key.
    /// </summary>
    public bool NumLock => GetKey((int)Key.NumLock);

    /// <summary>
    ///  The SCROLL LOCK key.
    /// </summary>
    public bool Scroll => GetKey((int)Key.Scroll);

    /// <summary>
    ///  The left SHIFT key.
    /// </summary>
    public bool LShiftKey => GetKey((int)Key.LShiftKey);

    /// <summary>
    ///  The right SHIFT key.
    /// </summary>
    public bool RShiftKey => GetKey((int)Key.RShiftKey);

    /// <summary>
    ///  The left CTRL key.
    /// </summary>
    public bool LControlKey => GetKey((int)Key.LControlKey);

    /// <summary>
    ///  The right CTRL key.
    /// </summary>
    public bool RControlKey => GetKey((int)Key.RControlKey);

    /// <summary>
    ///  The left ALT key.
    /// </summary>
    public bool LMenu => GetKey((int)Key.LMenu);

    /// <summary>
    ///  The right ALT key.
    /// </summary>
    public bool RMenu => GetKey((int)Key.RMenu);

    /// <summary>
    ///  The Browser Back key.
    /// </summary>
    public bool BrowserBack => GetKey((int)Key.BrowserBack);

    /// <summary>
    ///  The Browser Forward key.
    /// </summary>
    public bool BrowserForward => GetKey((int)Key.BrowserForward);

    /// <summary>
    ///  The Browser Refresh key.
    /// </summary>
    public bool BrowserRefresh => GetKey((int)Key.BrowserRefresh);

    /// <summary>
    ///  The Browser Stop key.
    /// </summary>
    public bool BrowserStop => GetKey((int)Key.BrowserStop);

    /// <summary>
    ///  The Browser Search key.
    /// </summary>
    public bool BrowserSearch => GetKey((int)Key.BrowserSearch);

    /// <summary>
    ///  The Browser Favorites key.
    /// </summary>
    public bool BrowserFavorites => GetKey((int)Key.BrowserFavorites);

    /// <summary>
    ///  The Browser Home key.
    /// </summary>
    public bool BrowserHome => GetKey((int)Key.BrowserHome);

    /// <summary>
    ///  The Volume Mute key.
    /// </summary>
    public bool VolumeMute => GetKey((int)Key.VolumeMute);

    /// <summary>
    ///  The Volume Down key.
    /// </summary>
    public bool VolumeDown => GetKey((int)Key.VolumeDown);

    /// <summary>
    ///  The Volume Up key.
    /// </summary>
    public bool VolumeUp => GetKey((int)Key.VolumeUp);

    /// <summary>
    ///  The Media Next Track key.
    /// </summary>
    public bool MediaNextTrack => GetKey((int)Key.MediaNextTrack);

    /// <summary>
    ///  The Media Previous Track key.
    /// </summary>
    public bool MediaPreviousTrack => GetKey((int)Key.MediaPreviousTrack);

    /// <summary>
    ///  The Media Stop key.
    /// </summary>
    public bool MediaStop => GetKey((int)Key.MediaStop);

    /// <summary>
    ///  The Media Play Pause key.
    /// </summary>
    public bool MediaPlayPause => GetKey((int)Key.MediaPlayPause);

    /// <summary>
    ///  The Launch Mail key.
    /// </summary>
    public bool LaunchMail => GetKey((int)Key.LaunchMail);

    /// <summary>
    ///  The Select Media key.
    /// </summary>
    public bool SelectMedia => GetKey((int)Key.SelectMedia);

    /// <summary>
    ///  The Launch Application1 key.
    /// </summary>
    public bool LaunchApplication1 => GetKey((int)Key.LaunchApplication1);

    /// <summary>
    ///  The Launch Application2 key.
    /// </summary>
    public bool LaunchApplication2 => GetKey((int)Key.LaunchApplication2);

    /// <summary>
    ///  The Oem Semicolon key.
    /// </summary>
    public bool OemSemicolon => GetKey((int)Key.OemSemicolon);

    /// <summary>
    ///  The Oem 1 key.
    /// </summary>
    public bool Oem1 => GetKey((int)Key.Oem1);

    /// <summary>
    ///  The Oem plus key.
    /// </summary>
    public bool Oemplus => GetKey((int)Key.Oemplus);

    /// <summary>
    ///  The Oem comma key.
    /// </summary>
    public bool Oemcomma => GetKey((int)Key.Oemcomma);

    /// <summary>
    ///  The Oem Minus key.
    /// </summary>
    public bool OemMinus => GetKey((int)Key.OemMinus);

    /// <summary>
    ///  The Oem Period key.
    /// </summary>
    public bool OemPeriod => GetKey((int)Key.OemPeriod);

    /// <summary>
    ///  The Oem Question key.
    /// </summary>
    public bool OemQuestion => GetKey((int)Key.OemQuestion);

    /// <summary>
    ///  The Oem 2 key.
    /// </summary>
    public bool Oem2 => GetKey((int)Key.Oem2);

    /// <summary>
    ///  The Oem 3 key.
    /// </summary>
    public bool Oem3 => GetKey((int)Key.Oem3);

    /// <summary>
    ///  The Oem tilde key.
    /// </summary>
    public bool Oemtilde => GetKey((int)Key.Oemtilde);

    /// <summary>
    ///  The Oem Open Brackets key.
    /// </summary>
    public bool OemOpenBrackets => GetKey((int)Key.OemOpenBrackets);

    /// <summary>
    ///  The Oem 4 key.
    /// </summary>
    public bool Oem4 => GetKey((int)Key.Oem4);

    /// <summary>
    ///  The Oem Pipe key.
    /// </summary>
    public bool OemPipe => GetKey((int)Key.OemPipe);

    /// <summary>
    ///  The Oem 5 key.
    /// </summary>
    public bool Oem5 => GetKey((int)Key.Oem5);

    /// <summary>
    ///  The Oem Close Brackets key.
    /// </summary>
    public bool OemCloseBrackets => GetKey((int)Key.OemCloseBrackets);

    /// <summary>
    ///  The Oem 6 key.
    /// </summary>
    public bool Oem6 => GetKey((int)Key.Oem6);

    /// <summary>
    ///  The Oem 7 key.
    /// </summary>
    public bool Oem7 => GetKey((int)Key.Oem7);

    /// <summary>
    ///  The Oem Quotes key.
    /// </summary>
    public bool OemQuotes => GetKey((int)Key.OemQuotes);

    /// <summary>
    ///  The Oem8 key.
    /// </summary>
    public bool Oem8 => GetKey((int)Key.Oem8);

    /// <summary>
    ///  The Oem 102 key.
    /// </summary>
    public bool Oem102 => GetKey((int)Key.Oem102);

    /// <summary>
    ///  The Oem Backslash key.
    /// </summary>
    public bool OemBackslash => GetKey((int)Key.OemBackslash);

    /// <summary>
    ///  The PROCESS KEY key.
    /// </summary>
    public bool ProcessKey => GetKey((int)Key.ProcessKey);

    /// <summary>
    ///  The Packet KEY key.
    /// </summary>
    public bool Packet => GetKey((int)Key.Packet);

    /// <summary>
    ///  The ATTN key.
    /// </summary>
    public bool Attn => GetKey((int)Key.Attn);

    /// <summary>
    ///  The CRSEL key.
    /// </summary>
    public bool Crsel => GetKey((int)Key.Crsel);

    /// <summary>
    ///  The EXSEL key.
    /// </summary>
    public bool Exsel => GetKey((int)Key.Exsel);

    /// <summary>
    ///  The ERASE EOF key.
    /// </summary>
    public bool EraseEof => GetKey((int)Key.EraseEof);

    /// <summary>
    ///  The PLAY key.
    /// </summary>
    public bool Play => GetKey((int)Key.Play);

    /// <summary>
    ///  The ZOOM key.
    /// </summary>
    public bool Zoom => GetKey((int)Key.Zoom);

    /// <summary>
    ///  A constant reserved for future use.
    /// </summary>
    public bool NoName => GetKey((int)Key.NoName);

    /// <summary>
    ///  The PA1 key.
    /// </summary>
    public bool Pa1 => GetKey((int)Key.Pa1);

    /// <summary>
    ///  The CLEAR key.
    /// </summary>
    public bool OemClear => GetKey((int)Key.OemClear);

    private readonly UInt128 _lowBits;
    private readonly UInt128 _highBits;

    private Keys(UInt128 lowBits, UInt128 highBits)
    {
        _lowBits = lowBits;
        _highBits = highBits;
    }

    private bool GetKey(int i)
    {
        return i < 128
            ? (_lowBits & (UInt128.One << i)) != UInt128.Zero
            : (_highBits & (UInt128.One << (i - 128))) != UInt128.Zero;
    }

    private Keys AddKey(int i)
    {
        return i < 128
            ? new Keys(_lowBits | (UInt128.One << i), _highBits)
            : new Keys(_lowBits, _highBits | (UInt128.One << (i - 128)));
    }
    
    public static Keys operator |(Keys left, Keys right) => new(left._lowBits | right._lowBits, left._highBits | right._highBits);
    public static Keys operator &(Keys left, Keys right) => new(left._lowBits | right._lowBits, left._highBits | right._highBits);
    
    public static Keys operator |(Keys left, Key right) => left.AddKey((int)right);
    public static bool operator &(Keys left, Key right) => left.GetKey((int)right);
    
    public static Keys operator ~(Keys keys) => new(~keys._lowBits, ~keys._highBits);

    public bool Equals(Keys other) => _lowBits.Equals(other._lowBits) && _highBits.Equals(other._highBits);
    public override bool Equals(object? obj) => obj is Keys other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (_lowBits.GetHashCode() * 397) ^ _highBits.GetHashCode();
        }
    }

    public static bool operator ==(Keys left, Keys right) => left.Equals(right);
    public static bool operator !=(Keys left, Keys right) => !left.Equals(right);

    public int CompareTo(Keys other)
    {
        var lowBitsComparison = _lowBits.CompareTo(other._lowBits);
        if (lowBitsComparison != 0) return lowBitsComparison;
        return _highBits.CompareTo(other._highBits);
    }

    public static bool operator <(Keys left, Keys right) => left.CompareTo(right) < 0;
    public static bool operator >(Keys left, Keys right) => left.CompareTo(right) > 0;
    public static bool operator <=(Keys left, Keys right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Keys left, Keys right) => left.CompareTo(right) >= 0;
}