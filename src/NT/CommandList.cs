using System;
using System.IO;
using System.Collections.Generic;

namespace NT
{
    public sealed class CommandBuffer : IDisposable {
        public Veldrid.CommandList commandList {get; private set;}
        public SharpDX.Direct3D11.DeviceContext1 context1 {get; private set;}

        public CommandBuffer() {
            commandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            var fields = commandList.GetType().GetField("_context1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance);
            context1 = fields.GetValue(commandList) as SharpDX.Direct3D11.DeviceContext1;
        }

        public static implicit operator Veldrid.CommandList(CommandBuffer v) => v.commandList;

        public void Dispose() {
            commandList.Dispose();
            context1 = null;
        }
    }
}