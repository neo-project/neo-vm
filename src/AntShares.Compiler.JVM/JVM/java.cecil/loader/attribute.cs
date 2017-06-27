using System;
using System.Collections.Generic;
using System.Text;

namespace javaloader
{
    [Flags]
    public enum Modifiers : ushort
    {
        Public = 0x0001,
        Private = 0x0002,
        Protected = 0x0004,
        Static = 0x0008,
        Final = 0x0010,
        Super = 0x0020,
        Synchronized = 0x0020,
        Volatile = 0x0040,
        Bridge = 0x0040,
        Transient = 0x0080,
        VarArgs = 0x0080,
        Native = 0x0100,
        Interface = 0x0200,
        Abstract = 0x0400,
        Strictfp = 0x0800,
        Synthetic = 0x1000,
        Annotation = 0x2000,
        Enum = 0x4000,

        // Masks
        AccessMask = Public | Private | Protected
    }


    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AnnotationDefaultAttribute : Attribute
    {
        public const byte TAG_ENUM = (byte)'e';
        public const byte TAG_CLASS = (byte)'c';
        public const byte TAG_ANNOTATION = (byte)'@';
        public const byte TAG_ARRAY = (byte)'[';
        public const byte TAG_ERROR = (byte)'?';
        private object defaultValue;

        // element_value encoding:
        // primitives:
        //   boxed values
        // string:
        //   string
        // enum:
        //   new object[] { (byte)'e', "<EnumType>", "<enumvalue>" }
        // class:
        //   new object[] { (byte)'c', "<Type>" }
        // annotation:
        //   new object[] { (byte)'@', "<AnnotationType>", ("name", (element_value))* }
        // array:
        //   new object[] { (byte)'[', (element_value)* }
        // error:
        //   new object[] { (byte)'?', "<exceptionClass>", "<exceptionMessage>" }
        public AnnotationDefaultAttribute(object defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public object Value
        {
            get
            {
                return defaultValue;
            }
        }
    }

}
