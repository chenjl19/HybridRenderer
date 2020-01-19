using System;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
	public static class FileSystem {
		public const string basePath = @"H:\neo - test_1 - 副本 (13)\assets";
		public const string assetBasePath = @"F:\ReEng\Assets";
		public const string shadersPath = @"shaders";

		public static string GetAssetPath(string name) {
			return Path.Combine(basePath, name);
		}

		public static string GetShaderAssetPath(string name) {
			return Path.Combine(basePath, shadersPath, name);
		}

		public static byte[] ReadAllBytes(string path) {
			string fullpath = Path.Combine(basePath, path);
			return File.ReadAllBytes(fullpath);
		}

		public static void ListFiles(string relativePath, string extension, Action<string> action) {
			string path = Path.Combine(basePath, relativePath);
			string[] files = Directory.GetFiles(path);
			if(files != null && files.Length > 0) {
				for(int i = 0; i < files.Length; i++) {
					if(Path.GetExtension(files[i]).ToLower() == extension.ToLower()) {
						action?.Invoke(files[i]);
					}
				}
			}			
		}

		public static List<string> ListFiles(string relativePath, string extension) {
			string path = Path.Combine(basePath, relativePath);
			if(!Directory.Exists(path)) {
				return null;
			}
			string[] files = Directory.GetFiles(path);
			List<string> listFiles = new List<string>();
			if(files != null && files.Length > 0) {
				for(int i = 0; i < files.Length; i++) {
					if(Path.GetExtension(files[i]).ToLower() == extension.ToLower()) {
						listFiles.Add(files[i]);
					}
				}
			}	
			return listFiles;		
		}

		public static void ListShaderFiles(Action<string> action, string extension = ".shader") {
			string path = Path.Combine(basePath, shadersPath);
			string[] files = Directory.GetFiles(path);
			if(files != null && files.Length > 0) {
				for(int i = 0; i < files.Length; i++) {
					if(Path.GetExtension(files[i]).ToLower() == extension.ToLower()) {
						action?.Invoke(files[i]);
						Console.WriteLine(files[i]);
					}
				}
			}
		}

		public static string CreateOSPath(string filename) {
			return Path.Combine(basePath, filename);
		}

		public static string CreateAssetOSPath(string filename) {
			return Path.Combine(assetBasePath, filename);
		}

		public static string CreateAssetOSPath(string filename, string ext) {
			return Path.Combine(assetBasePath, filename) + ext;
		}
	}
}