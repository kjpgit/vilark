namespace vilark;

public static class EnumExtensions
{
    public static T IncrementEnum<T>(this T src, int delta) where T : struct, System.Enum
    {
        T[] allValues = (T[])Enum.GetValuesAsUnderlyingType(src.GetType());
        int new_index = Array.IndexOf<T>(allValues, src) + delta;
        if (new_index < 0) {
            new_index = allValues.Length - 1;
        } else if (new_index >= allValues.Length) {
            new_index = 0;
        }
        return allValues[new_index];
    }
}
