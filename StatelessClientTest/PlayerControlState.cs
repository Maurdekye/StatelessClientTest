namespace StatelessClientTest
{
    public class PlayerControlState
    {
        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Sprinting { get; set; }

        public PlayerControlState()
        {
            Up = false;
            Down = false;
            Left = false;
            Right = false;
            Sprinting = false;
        }
    }
}
