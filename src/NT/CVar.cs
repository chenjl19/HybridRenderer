using System;
using System.IO;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    public abstract class CVar {
        [Flags]
        public enum Flags {
            None,
            Dev = 1 << 0,
            Color = 1 << 1,
            Modified = 1 << 2
        }

        public Flags flags {get; protected set;}
        public string name {get; protected set;}
        public string description {get; protected set;}
        public bool IsModified() => flags.HasFlag(Flags.Modified);
        public void SetModified(bool value) {
            if(value) {
                flags |= Flags.Modified;
            } else {
                flags &= ~Flags.Modified;
            }
        }

        public CVar(string _name, string _description, Flags _flags) {
            flags = _flags;
            name = _name;
            description = _description;
        }
    }

    public sealed class CVarString : CVar {
        public string value {get; private set;}
        public string resetValue {get; private set;}

        public CVarString(string _name, string _value, string _description, Flags _flags) : base(_name, _description, _flags) {
            value = _value;
            resetValue = value;    
        }
    }

    public sealed class CVarBool : CVar {
        public bool value {get; private set;}
        bool resetValue;

        public void Set(bool v) {
            value = v;
        }

        public CVarBool(string _name, bool _value, string _description, Flags _flags) : base(_name, _description, _flags) {
            value = _value;
            resetValue = value;
        }        
    }

    public sealed class CVarInteger : CVar {
        public int value {get; private set;}
        public int resetValue {get; private set;}
        public int minValue {get; private set;}
        public int maxValue {get; private set;}
        public int speed {get; private set;}

        public void Set(int _value) {
            value = Math.Clamp(_value, minValue, maxValue);
        }

        public void Reset() {
            value = resetValue;
        }

        public CVarInteger(string _name, int _value, string _description, Flags _flags, int _minValue, int _maxValue, int _speed = 1) : base(_name, _description, _flags) {
            minValue = _minValue;
            maxValue = _maxValue;
            speed = _speed;
            Set(_value);    
            resetValue = value;    
        }
    }

    public sealed class CVarFloat : CVar {
        public float value {get; private set;}
        public float resetValue {get; private set;}
        public float minValue {get; private set;}
        public float maxValue {get; private set;}
        public float speed {get; private set;}

        public void Set(float _value) {
            value = Math.Clamp(_value, minValue, maxValue);
        }

        public CVarFloat(string _name, float _value, string _description, Flags _flags, float _minValue, float _maxValue, float _speed = 0.1f) : base(_name, _description, _flags) {
            minValue = _minValue;
            maxValue = _maxValue;
            speed = _speed;
            Set(_value);
            resetValue = value;    
        }
    }

    public sealed class CVarVector : CVar {
        public Vector4 value {get; private set;}
        public Vector4 resetValue {get; private set;}
        public float speed {get; private set;}

        public void Set(Vector4 _value) {
            value = _value;
        }

        public CVarVector(string _name, Vector4 _value, string _description, Flags _flags, float _speed = 0.1f) : base(_name, _description, _flags) {
            speed = _speed;
            Set(_value);
            resetValue = value;    
        }
    } 
}