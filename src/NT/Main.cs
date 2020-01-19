using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using Veldrid.Sdl2;
using Veldrid.Utilities;
using Veldrid.StartupUtilities;
using ImGuiNET;

namespace NT 
{
    public static class Common {
        public readonly static DeclManager declManager;
        public readonly static FrameGraph frameGraph;

        static Common() {
            declManager = new DeclManager();
            frameGraph = new FrameGraph();
        }

        public static void Init() {
            frameGraph.SetupPasses();
            declManager.Init();
            frameGraph.PostInit();
        }

        public static void Shutdown() {
            declManager.Shutdown();
            frameGraph.Shutdown();
        }
    }

    class Program {

        static void Run() {
            WindowCreateInfo windowCreateInfo = new WindowCreateInfo {
                X = 50,
                Y = 50,
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowInitialState = Veldrid.WindowState.Normal,
                WindowTitle = "Demo"
            };
            Sdl2Window mainWindow = VeldridStartup.CreateWindow(ref windowCreateInfo);
            
            GraphicsDevice.Init();

            Veldrid.SwapchainDescription mainSwapchainDesc = new Veldrid.SwapchainDescription(
                VeldridStartup.GetSwapchainSource(mainWindow),
                (uint)mainWindow.Width,
                (uint)mainWindow.Height,
                null,
                true,
                false);
            Veldrid.Swapchain mainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(ref mainSwapchainDesc);   

            InputModule inputModule = new InputModule();
            UserInput userInput = new UserInput(inputModule);

            bool windowResized = false;
            mainWindow.Resized += () => {windowResized = true;};

            SceneRenderer sceneRenderer = new SceneRenderer(Common.frameGraph, mainSwapchain);

            Common.Init();
            Common.declManager.RegisterDeclFolder(Path.Combine(FileSystem.assetBasePath, "materials"), DeclType.Material, ".material");
            Common.declManager.RegisterDeclFolder(Path.Combine(FileSystem.assetBasePath, "particles"), DeclType.Material, ".prt");
            Editor editor = new Editor();

            var imGuiRenderer = new Veldrid.ImGuiRenderer(GraphicsDevice.gd, mainSwapchain.Framebuffer.OutputDescription, mainWindow.Width, mainWindow.Height);

            CommandBuffer mainCommandBuffer = new CommandBuffer();

            sceneRenderer.Init();

            var r = Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(30f)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(1f)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(-75f));
            Console.WriteLine(r.ToString("f6"));

            while(mainWindow.Exists) {
                TimeSystem.frameCount++;
                TimeSystem.Update();
                FrameAllocator.NewFrame();

                var inputSnapshot = mainWindow.PumpEvents();
                inputModule.ProcessEvents(inputSnapshot);
                imGuiRenderer.Update(Time.delteTime, inputSnapshot);

                if(!mainWindow.Exists) {
                    break;
                }

                // Update
                {
                    editor.Update(userInput, mainWindow);
                }

                Scene.GlobalScene.GetRenderData(out SceneRenderData sceneRenderData);

                if(windowResized) {
                    GraphicsDevice.gd.WaitForIdle();
                    mainSwapchain.Dispose();
                    mainSwapchainDesc.Width = (uint)mainWindow.Width;
                    mainSwapchainDesc.Height = (uint)mainWindow.Height;
                    mainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(ref mainSwapchainDesc);
                    sceneRenderer.OnMainSwapchainResized(mainSwapchain);
                    imGuiRenderer.WindowResized((int)mainSwapchainDesc.Width, (int)mainSwapchainDesc.Height);
                    GraphicsDevice.gd.WaitForIdle();
                    windowResized = false;
                }

                Common.frameGraph.BackendBeginFrame();

                Veldrid.CommandList mainCL = mainCommandBuffer.commandList;
                mainCL.Begin();
                Scene.GlobalScene.meshRenderSystem.UpdateMeshes_RenderThread(mainCL);
                sceneRenderer.Render(mainCommandBuffer, sceneRenderData);
                imGuiRenderer.Render(GraphicsDevice.gd, mainCL);
                mainCL.End();

                Common.frameGraph.BackendEndFrame();

                GraphicsDevice.gd.SubmitCommands(mainCL);
                GraphicsDevice.gd.SwapBuffers(mainSwapchain);
            }
        }

        [STAThread]
        static void Main(string[] args) {
            //try {
                Run();
            //} catch(Exception e) {
            //    Console.WriteLine(e.StackTrace);
            //    Console.WriteLine(e.Message);
            //}


/*
            WindowCreateInfo windowCreateInfo = new WindowCreateInfo {
                X = 50,
                Y = 50,
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowInitialState = Veldrid.WindowState.Normal,
                WindowTitle = "Demo"
            };
            Sdl2Window mainWindow = VeldridStartup.CreateWindow(ref windowCreateInfo);

            GraphicsDevice.Init();

            Veldrid.SwapchainDescription mainSwapchainDesc = new Veldrid.SwapchainDescription(
                VeldridStartup.GetSwapchainSource(mainWindow),
                (uint)mainWindow.Width,
                (uint)mainWindow.Height,
                null,
                true,
                false);
            Veldrid.Swapchain mainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(ref mainSwapchainDesc);

            //LightingPass lightingPass = new LightingPass(Common.frameGraph, "LightingPass");
            //DepthPass depthPass = new DepthPass(Common.frameGraph, "DepthPass");
            PresentPass presentPass = new PresentPass(Common.frameGraph, mainSwapchain.Framebuffer, "PresentPass");
            //DebugPass debugPass = new DebugPass(Common.frameGraph, lightingPass, "DebugPass");
            //Common.frameGraph.AddRenderPass(lightingPass);
            //Common.frameGraph.AddRenderPass(depthPass);
            //Common.frameGraph.AddRenderPass(debugPass);
            SceneRenderer3D sceneRenderer3D = new SceneRenderer3D(Common.frameGraph);
            Common.frameGraph.AddRenderPass(presentPass);
            Common.Init();

            Veldrid.ImGuiRenderer imguiRenderer = new Veldrid.ImGuiRenderer(GraphicsDevice.gd, mainSwapchain.Framebuffer.OutputDescription, mainWindow.Width, mainWindow.Height);

            bool windowResized = false;
            mainWindow.Resized += () => {windowResized = true;};

            Matrix projectionMatrix = Matrix.PerspectiveFovRH(SharpDX.MathUtil.DegreesToRadians(60f), (float)mainWindow.Width / (float)mainWindow.Height, 0.3f, 500f);

            InputModule inputModule = new InputModule();
            UserInput userInput = new UserInput(inputModule);
            AroundViewController aroundView = new AroundViewController();

            GameObject player = Common.game.SpawnGameObject<GameObject>("Player");
            CameraComponent playerView = new CameraComponent(player);
            playerView.Register();

            RenderModelLevel levelModel = Common.renderModelManager.CreateModel<RenderModelLevel>("_Level:Sponza");
            levelModel.InitFromFile(@"E:\src\DeferredTexturing-master\Content\Models\Sponza\Sponza.fbx", -1);
            Material[] levelMaterials = new Material[levelModel.NumMeshes];
            for(int i = 0; i < levelMaterials.Length; i++) {
                levelMaterials[i] = Common.declManager.FindMaterial($"SponzaMaterial{i}");
            }
            levelModel.Update();

            Common.game.renderWorld.AddLevel(levelModel, levelMaterials, Vector3.Zero, Quaternion.Identity);

            while(mainWindow.Exists) {
                TimeSystem.Update();
                FrameAllocator.NewFrame();

                var inputSnapshot = mainWindow.PumpEvents();
                inputModule.ProcessEvents(inputSnapshot);

                if(!mainWindow.Exists) {
                    break;
                }
                if(windowResized) {
                    int width = mainWindow.Width;
                    int height = mainWindow.Height;
                    GraphicsDevice.gd.WaitForIdle();
                    mainSwapchain.Dispose();
                    mainSwapchainDesc.Width = (uint)width;
                    mainSwapchainDesc.Height = (uint)height;
                    mainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(ref mainSwapchainDesc);
                    presentPass.SetFramebuffer(mainSwapchain.Framebuffer);
                    imguiRenderer.WindowResized(width, height);
                    windowResized = false;
                }
                
                aroundView.Update(userInput, Time.delteTime, mainWindow.Width, mainWindow.Height);

                imguiRenderer.Update(TimeSystem.deltaTime, inputSnapshot);
                ImGui.ShowDemoWindow();

                Vector3 origin = aroundView.GetPosition();
                Vector3 forward = aroundView.GetForward();
                Vector3 right = Vector3.Cross(forward, MathHelper.Vec3Up);
                Vector3 up = Vector3.Cross(right, forward);
                right.Normalize();
                up.Normalize();
                playerView.SetWorldPosition(origin);
                playerView.SetAxis(right, forward, up);

                var planeN = Vector3.Transform(MathHelper.Vec3Forward, Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(-45f)));
                Common.renderDebug.AddPlane(new Plane(Vector3.Zero, planeN), 20f, Color.Yellow);

                Common.game.renderWorld.GetRenderData(out SceneRenderData sceneRenderData);
                Common.renderDebug.GetRenderData(out DebugToolRenderData debugToolRenderData);

                Veldrid.CommandList cl = Common.frameGraph.mainDrawCommandList;

                Common.frameGraph.frameRenderResources.MapDynamicBuffers();
                Common.renderModelManager.UpdateResources_RenderThread();

                Common.frameGraph.mainDrawCommandList.Begin();

                //lightingPass.Execute(sceneRenderData);
                //debugPass.Execute(debugToolRenderData);
                sceneRenderer3D.Render(sceneRenderData, debugToolRenderData);
                presentPass.Execute(imguiRenderer);

                Common.frameGraph.EndFrame();
                GraphicsDevice.gd.SubmitCommands(cl);

                GraphicsDevice.gd.SwapBuffers(mainSwapchain);
            }

            GraphicsDevice.gd.WaitForIdle();
            Common.Shutdown();
            imguiRenderer.Dispose();
            mainSwapchain.Dispose();
            GraphicsDevice.Shutdown();
*/
        }
    }
}