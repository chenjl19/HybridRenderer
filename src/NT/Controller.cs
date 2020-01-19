using System;
using System.IO;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    public static class EditorController {
        static EditorController() {
            origin = new Vector3(0f, -1f, 1.5f);
            right = MathHelper.Vec3Right;
            forward = MathHelper.Vec3Forward;
            up = MathHelper.Vec3Up;
            rotation = Quaternion.Identity;
        }

        public static Vector3 origin;
        public static Vector3 right;
        public static Vector3 forward;
        public static Vector3 up;

        static Quaternion rotation;
        static bool draging;
        static bool dragingGUI;
        static int grabX, grabY;
        static Vector2 axis;
        static float fpsYawAngle;
        static float fpsPitchAngle;
        static float yawAngle;

        const int MoveButton = 0;
        const int AngleButton = 2;

        static void SetCursorPos(Veldrid.Sdl2.Sdl2Window window, int x, int y) {
            var pt = window.ClientToScreen(new Veldrid.Point(x, y));
            Win32.SetCursorPos(pt.X, pt.Y);
        }

        public static void Update(UserInput userInput, Veldrid.Sdl2.Sdl2Window window) {
            if(dragingGUI) {
                if(userInput.GetMouseButtonUp(0)) {
                    dragingGUI = false;
                } else {
                    return;
                }
            }

            if(!draging && (userInput.GetMouseButton(MoveButton) || userInput.GetMouseButton(AngleButton))) {
                bool anyItemHovered = ImGuiNET.ImGui.IsAnyItemHovered();
                bool windowCollapsed = ImGuiNET.ImGui.IsWindowCollapsed();
                bool windowHovered = ImGuiNET.ImGui.IsWindowHovered(ImGuiNET.ImGuiHoveredFlags.AnyWindow);
                if((anyItemHovered || windowCollapsed || windowHovered)) {
                    if(userInput.GetMouseButton(0)) {
                        dragingGUI = true;
                    }
                    return;
                } else {
                    draging = true;
                    grabX = (int)userInput.mousePosition.X;
                    grabY = (int)userInput.mousePosition.Y;
                    window.CursorVisible = false;     
                }
            }

            if(draging) {
                Vector2 mouseDelta = userInput.mousePosition - new Vector2(grabX, grabY);
                axis.X = MathF.Sign(mouseDelta.X);
                axis.Y = MathF.Sign(mouseDelta.Y);

                if(userInput.GetMouseButton(MoveButton)) {
                    if(userInput.GetMouseButton(AngleButton)) {
                        origin += right * mouseDelta.X * 0.25f * Time.delteTime;
                        origin -= MathHelper.Vec3Up * mouseDelta.Y * 0.25f * Time.delteTime;
                    } else {
                        yawAngle -= mouseDelta.X * 3f * Time.delteTime;
                        Quaternion yawRot = Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(yawAngle + fpsYawAngle));
                        rotation *= Quaternion.Invert(rotation) * (yawRot * Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(fpsPitchAngle)));
                        origin -= Vector3.Transform(MathHelper.Vec3Forward, yawRot) * mouseDelta.Y * Time.delteTime; 

                    }
                } else if(userInput.GetMouseButton(AngleButton)) {
                    fpsPitchAngle -= mouseDelta.Y * 3f * Time.delteTime;
                    fpsYawAngle -= mouseDelta.X * 3f * Time.delteTime;
                    rotation = Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(fpsYawAngle + yawAngle)) * Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(fpsPitchAngle));
                }

                forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
                right = Vector3.Transform(MathHelper.Vec3Right, rotation);
                up = Vector3.Transform(MathHelper.Vec3Up, rotation);
 
                //userInput.SetMousePosition(grabX, grabY);  
                SetCursorPos(window, grabX, grabY);
            }

            if (draging && (userInput.GetMouseButtonUp(MoveButton) || userInput.GetMouseButtonUp(AngleButton))) {
                draging = false;
                SetCursorPos(window, grabX, grabY);  
                window.CursorVisible = true;
            }
        }
    }

    public class AroundViewController {
        public Vector3 position;
        public Vector3 targetPosition;
        public float minDolly = 1.0f;
        public float maxDolly = 20.0f;
        public float orbitSpeed = 8f;
        public float dollySpeed = 8f;
	    Vector2 prevMousePosition;
        float dolly;
        Vector2 orbit = Vector2.Zero;
        Vector3 right;
        Vector3 forward;
        Vector3 up;

        public AroundViewController() {
            Reset();
        }

        public void Reset() {
            position = new Vector3(0.0f, -2.5f, 0f);
            targetPosition = new Vector3(0f, 0f, 0f);
            forward = Vector3.Normalize(targetPosition - position);
        }

        void Orbit(float dx, float dy) {
            orbit.X += dx;
            orbit.Y += dy;
        }

        void Dolly(float dz) {
            position += forward * dz;
        }

        void UpdateOrbit(float amount) {
            Vector2 consume = new Vector2(orbit.X * amount, orbit.Y * amount);
            orbit.X -= consume.X;
            orbit.Y -= consume.Y;

            Vector3 toPosition = position - targetPosition;
            float toPositionLength = toPosition.Length();
            Vector3 toPositionNormal = Vector3.Normalize(toPosition);

            Vector2 uv = Vector2.Zero;
            MathHelper.LatLongFromVector3(ref uv.X, ref uv.Y, toPositionNormal);
            uv.X += consume.X;
            uv.Y = MathUtil.Clamp(uv.Y - consume.Y, 0.02f, 0.98f);

            Vector3 toDestDir = Vector3.Zero;
            MathHelper.Vector3FromLatLong(ref toDestDir, uv.X, uv.Y);
            position += (toDestDir - toPositionNormal) * toPositionLength;
            forward = Vector3.Normalize((targetPosition - position));
        }

        public void Update(UserInput input, float dt, int screenWidth, int screenHeight) {
            if(!dragBegin && (ImGui.IsAnyItemHovered() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))) {
                return;
            }

            float amount = MathF.Min(dt / 0.12f, 1.0f);

            if(input.GetMouseButton(0) || input.GetMouseButton(1)) {
                if(!dragBegin) {
                    dragBegin = true;
                    prevMousePosition = input.mousePosition;                    
                }
            }

            Vector2 mouseDelta = input.mousePositionDelta;

            if(dragBegin && input.GetMouseButton(0)) {
                Orbit(mouseDelta.X / (float)screenWidth, mouseDelta.Y / (float)screenHeight);                
            }

            if(dragBegin && input.GetMouseButton(1)) {
                Dolly((mouseDelta.X / (float)screenWidth + mouseDelta.Y / (float)screenHeight) * 10f);
            }

            UpdateOrbit(amount);

            if (input.GetMouseButtonUp(0) || input.GetMouseButtonUp(1)) {
                dragBegin = false;
            }
        }

        bool dragBegin;
        public Vector3 GetPosition() { return position; }
        public Vector3 GetForward() { return forward; }
    }
}