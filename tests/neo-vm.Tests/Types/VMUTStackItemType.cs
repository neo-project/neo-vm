namespace Neo.Test.Types
{
    public enum VMUTStackItemType
    {
        /// <summary>
        /// Boolean (true,false)
        /// </summary>
        Boolean,
        
        /// <summary>
        /// ByteArray
        /// </summary>
        ByteArray,
        
        /// <summary>
        /// ByteArray as UTF8 string
        /// </summary>
        String,
        
        /// <summary>
        /// String
        /// </summary>
        Interop,
        
        /// <summary>
        /// BigInteger
        /// </summary>
        Integer,
        
        /// <summary>
        /// Array
        /// </summary>
        Array,

        /// <summary>
        /// Struct
        /// </summary>
        Struct,

        /// <summary>
        /// Map
        /// </summary>
        Map
    }
}