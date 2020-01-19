using System;
using SharpDX;
using DInput = SharpDX.DirectInput;

namespace NT
{    
    public class UserInput {
        public bool anyKey {get {return inputModule.anyKey;}}
        public bool anyKeyDown {get {return inputModule.anyKeyDown;}}
        public Vector2 mousePosition {get {return inputModule.mousePosition;}}
        public Vector2 mousePositionDelta {get {return inputModule.mousePositionDelta;}}
        public float mouseWheelDelta {get {return inputModule.mouse.wheelDelta;}}

        readonly InputModule inputModule;

        public UserInput(InputModule module) {
            inputModule = module;
        }

        public Vector2 GetMouseAxisRaw() {
            return inputModule.mouseAxisRaw;
        }

        public bool GetKey(DInput.Key keyCode) {
            return inputModule.keys[(int)keyCode].down;
        }

        public bool GetKeyDown(DInput.Key keyCode) {
            int id = (int)keyCode;
            return inputModule.keys[id].down && inputModule.keys[id].frameNum == Time.frameCount;
        }

        public bool GetKeyUp(DInput.Key keyCode) {
            int id = (int)keyCode;
            return !inputModule.keys[id].down && inputModule.keys[id].frameNum == Time.frameCount;
        }

        public bool GetMouseButton(int button) {
            if(button >= 0 && button < 8) {
                return inputModule.buttons[button].down;
            }
            return false;            
        }

        public bool GetMouseButtonDown(int button) {
            if(button >= 0 && button < 8) {
                return inputModule.buttons[button].down && inputModule.buttons[button].frameNum == Time.frameCount;
            }
            return false;
        }

        public bool GetMouseButtonUp(int button) {
            if(button >= 0 && button < 8) {
                return !inputModule.buttons[button].down && inputModule.buttons[button].frameNum == Time.frameCount;
            }
            return false;
        }

        public void SetMousePosition(int x, int y) {
            inputModule.SetMousePosition(x, y);
        }
    }

    public class InputModule {
        public struct Key
        {
            public bool down;
            public uint frameNum;
        }

        public struct MosueButton {
            public bool down;
            public uint frameNum;
        }

        public struct Mouse
        {
            public int dx;
            public int dy;
            public int wheelDelta;
            public int screenX;
            public int screenY;
        }

        public Key[] keys = new Key[256];
        public MosueButton[] buttons = new MosueButton[8];
        public Mouse mouse = new Mouse();
        public bool anyKey;
        public bool anyKeyDown;
        public Vector2 mousePosition;
        public Vector2 mousePositionDelta;
        public Vector2 mouseAxisRaw;
        Vector2 oldMousePosition;
        
        public void SetMousePosition(int x, int y) {
            mousePosition.X = x;
            mousePosition.Y = y;
        }

        public InputModule() {
            for (int i = 0; i < keys.Length; i++) {
                keys[i].frameNum = uint.MaxValue;
            }
        }

        public void ProcessEvents(Veldrid.InputSnapshot inputSnapshot) {
            anyKeyDown = false;
            var keyEvents = inputSnapshot.KeyEvents;
            var mouseEvents = inputSnapshot.MouseEvents;

            mouse.screenX = (int)inputSnapshot.MousePosition.X;
            mouse.screenY = (int)inputSnapshot.MousePosition.Y;
            mousePosition = new Vector2(inputSnapshot.MousePosition.X, inputSnapshot.MousePosition.Y);
            mousePositionDelta = mousePosition - oldMousePosition;
            mouseAxisRaw.X = MathF.Sign(mousePosition.X - oldMousePosition.X);
            mouseAxisRaw.Y = MathF.Sign(mousePosition.Y - oldMousePosition.Y);
            oldMousePosition = mousePosition;
            mouse.wheelDelta = (int)inputSnapshot.WheelDelta;

            foreach(var ev in mouseEvents) {
                buttons[(int)ev.MouseButton].down = ev.Down;
                buttons[(int)ev.MouseButton].frameNum = Time.frameCount;
            }

            foreach(var ev in keyEvents) {
                int code = (int)ev.Key;
                keys[code].down = ev.Down;
                keys[code].frameNum = Time.frameCount;
                if (anyKey == false && keys[code].down) {
                    anyKey = true;
                    anyKeyDown = true;
                } else {
                    anyKey = false;
                }
            }      
        }
    }
}