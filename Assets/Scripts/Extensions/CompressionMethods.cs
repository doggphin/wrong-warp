public static class CompressionHelpers {
    public static byte CompressUnsignedFloat(float val, float maxValue) {
        return (byte)(byte.MaxValue * (val / maxValue));
    }
    public static float DecompressUnsignedFloat(byte val, float maxValue) {
        return val * (maxValue / byte.MaxValue);
    }

    
    public static byte CompressNormalizedFloat(float val) {
        return (byte)((val + 1) * 127.5);
    }
    public static float DecompressNormalizedFloat(byte val) {
        return (val / 127.5f) - 1f;
    }
}