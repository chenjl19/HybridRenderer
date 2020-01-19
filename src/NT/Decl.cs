using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using SharpDX;

namespace NT
{
    public enum DeclType {
        Shader,
        Material,
        ComputeShader,
        ParticleSystem,
        NumTypes
    }

    public class DeclTypeInfo {
        public string typeName;
        public DeclType typeEnum;
        public Type type;
    }

    public class DeclFolder {
        public string folder;
        public string extension;
        public DeclType defaultType;
    }

    public class DeclFile {
        readonly DeclManager manager;

        public DeclFile(DeclManager manager, string filename, DeclType type) {
            this.manager = manager;
            this.filename = filename;
            this.type = type;
        }

        public void LoadAndParse() {
            Token token = new Token();
            Lexer lex = new Lexer(System.IO.File.ReadAllText(filename));

            int startMarker = 0;
            int sourceLine = 0;
            int size = 0;

            while(true) {
                startMarker = lex.GetFileOffset();
                sourceLine = lex.GetLineNum();

                // parse type name
                if(!lex.ReadToken(ref token)) {
                    break;
                }
                
                DeclTypeInfo identifiedTypeInfo = manager.GetDeclTypeInfo(token.lexme);
                if(identifiedTypeInfo == null) {
                    break;
                }

                // parse name
                if(!lex.ReadToken(ref token)) {
                    break;
                }
                string name = token.lexme;

                // make sure there's a '{'
                if(!lex.ReadToken(ref token)) {
                    continue;
                }
                if(token.lexme != "{") {
                    break;
                }
                lex.UnreadToken(ref token);

                lex.SkipBracedSection();
                size = lex.GetFileOffset() - startMarker;

                Decl newDecl = manager.FindTypeWithoutParsing(identifiedTypeInfo.typeEnum, name);

                newDecl.SetText(lex.GetText(startMarker, size));
                newDecl.sourceLine = lex.GetLineNum();
                newDecl.sourceTextLength = size;
                newDecl.sourceTextOffset = startMarker;
                newDecl.Parse();
            }
        }

        public readonly string filename;
        readonly DeclType type;
    }

    public abstract class Decl : IDisposable {
        public string name;
        public DeclType typeEnum;
        public DeclFile sourceFile;
        public int sourceTextOffset;
        public int sourceTextLength;
        public int sourceLine;
        protected char[] text;

        internal virtual void ReplaceSourceFileText() {
            if(text == null || sourceFile == null) {
                return;
            }

            File.WriteAllBytes(sourceFile.filename, UTF8Encoding.UTF8.GetBytes(text));
        }

        internal abstract void Parse();

        public void SetText(char[] inText) {
            text = inText;
        }

        public abstract void Dispose();

        public static string VectorToString(Vector2 v) {
            return $"{v.X} {v.Y}";
        }

        public static string VectorToString(Vector3 v) {
            return $"{v.X} {v.Y} {v.Z}";
        }

        public static string VectorToString(Vector4 v) {
            return $"{v.X} {v.Y} {v.Z} {v.W}";
        }
    }

    public class DeclManager {
        List<DeclFolder> declFolders;
        DeclTypeInfo[] declTypeInfos;
        Dictionary<string, DeclFile> loadedFiles;
        Dictionary<string, Decl>[] loadedDecls;

        public int numTypes => declTypeInfos.Length;

        public DeclTypeInfo GetDeclTypeInfo(string typeName) {
            for(int i = 0; i < declTypeInfos.Length; i++) {
                if(declTypeInfos[i] != null && declTypeInfos[i].typeName == typeName) {
                    return declTypeInfos[i];
                }
            }
            return null;
        }

        public DeclManager() {
            declFolders = new List<DeclFolder>();
            declTypeInfos = new DeclTypeInfo[(int)DeclType.NumTypes];
            loadedFiles = new Dictionary<string, DeclFile>();
            loadedDecls = new Dictionary<string, Decl>[(int)DeclType.NumTypes];
            for(int i = 0; i < loadedDecls.Length; i++) {
                loadedDecls[i] = new Dictionary<string, Decl>();
            }
            
            RegisterDeclType("material", DeclType.Material, typeof(Material));
            RegisterDeclType("shader", DeclType.Shader, typeof(Shader));
            RegisterDeclType("compute", DeclType.ComputeShader, typeof(ComputeShader));
            RegisterDeclType("particleSystem", DeclType.ParticleSystem, typeof(ParticleSystem));
        }

        public void Init() {
            RegisterDeclFolder("shaders", DeclType.Shader, ".shader");
            //RegisterDeclFolder("materials", DeclType.Material, ".material");
            RegisterDeclFolder("shaders", DeclType.ComputeShader, ".compute");
        }

        public void Shutdown() {
            if(loadedDecls != null) {
                for(int i = 0; i < loadedDecls.Length; i++) {
                    var decls = loadedDecls[i];
                    foreach(var decl in decls) {
                        decl.Value.Dispose();
                    }
                    decls.Clear();
                }
                loadedDecls = null;
            }
        }

        public void RegisterDeclType(string typeName, DeclType typeEnum, Type type) {
            if((int)typeEnum < declTypeInfos.Length && declTypeInfos[(int)typeEnum] != null) {
                return;
            }

            DeclTypeInfo info = new DeclTypeInfo(); 
            info.typeName = typeName;
            info.typeEnum = typeEnum;
            info.type = type;
            declTypeInfos[(int)typeEnum] = info;
        }

        public void RegisterDeclFile(string filename, DeclType type) {
            if(!loadedFiles.TryGetValue(filename, out DeclFile file)) {
                file = new DeclFile(this, filename, type);
                file.LoadAndParse();
                loadedFiles.Add(filename, file);
            }
        }

        public void RegisterDeclFolder(string folder, DeclType type, string extension) {
            int i = 0;
            DeclFolder declFolder = null;

            for(; i < declFolders.Count; i++) {
                if(declFolders[i].folder == folder && declFolders[i].extension == extension) {
                    break;
                }
            }

            if(i < declFolders.Count) {
                declFolder = declFolders[i];
            } else {
                declFolder = new DeclFolder();
                declFolder.folder = folder;
                declFolder.extension = extension;
                declFolder.defaultType = type;
            }

            List<string> fileList = FileSystem.ListFiles(declFolder.folder, declFolder.extension);
            for(i = 0; i < fileList.Count; i++) {
                if(loadedFiles.TryGetValue(fileList[i], out DeclFile declFile)) {
                    continue;
                }

                declFile = new DeclFile(this, fileList[i], type);
                loadedFiles.Add(fileList[i], declFile);
                declFile.LoadAndParse();
            }
        }

        public Decl CreateNewDecal(DeclType type, string name, string filename) {
            int typeIndex = (int)type;

            if(type < 0 || typeIndex >= declTypeInfos.Length || declTypeInfos[typeIndex] == null || typeIndex >= (int)DeclType.NumTypes) {
                return null;
            }

            if(loadedDecls[typeIndex].TryGetValue(name, out Decl decl)) {
                return decl;
            }

            if(!loadedFiles.TryGetValue(filename, out DeclFile sourceFile)) {
                sourceFile = new DeclFile(this, filename, type);
                loadedFiles.Add(filename, sourceFile);
            }         

            DeclTypeInfo typeInfo = declTypeInfos[typeIndex];
            decl = Activator.CreateInstance(typeInfo.type) as Decl;
            decl.name = name;
            decl.typeEnum = type;
            decl.sourceFile = sourceFile;

            return decl;
        }

        public Decl FindTypeWithoutParsing(DeclType type, string name) {
            int typeIndex = (int)type;

            if(type < 0 || typeIndex >= declTypeInfos.Length || declTypeInfos[typeIndex] == null || typeIndex >= (int)DeclType.NumTypes) {
                return null;
            }

            if(loadedDecls[typeIndex].TryGetValue(name, out Decl decl)) {
                return decl;
            }

            DeclTypeInfo typeInfo = declTypeInfos[typeIndex];
            decl = Activator.CreateInstance(typeInfo.type) as Decl;
            decl.name = name;
            decl.typeEnum = type;

            loadedDecls[typeIndex].Add(name, decl);
            return decl;
        }   

        public Decl FindByType(string name, DeclType type) {
            if(!loadedDecls[(int)type].TryGetValue(name, out Decl decl)) {
                return null;
            }
            return decl;                 
        }

        public Shader FindShader(string name) {
            if(!loadedDecls[(int)DeclType.Shader].TryGetValue(name, out Decl shader)) {
                return null;
            }
            return shader as Shader;            
        }

        public ComputeShader FindComputeShader(string name) {
            if(!loadedDecls[(int)DeclType.ComputeShader].TryGetValue(name, out Decl shader)) {
                return null;
            }
            return shader as ComputeShader;                    
        }

        public Material FindMaterial(string name) {
            if(!loadedDecls[(int)DeclType.Material].TryGetValue(name, out Decl material)) {
                return null;
            }
            return material as Material;
        }
    }
}