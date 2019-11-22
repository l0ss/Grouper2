namespace Grouper2.Utility
{
    public static class Extensions
    {
        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start index, exclusive for end index.
        /// </summary>
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }
        
        /// <summary>
        /// Imitates the powershell arr[i..j] functionality.
        /// ... Inclusive for start index, inclusive for end index.
        ///
        /// $a = 0,1,2,3,4,5
        /// 
        /// > Write-Host $a[0..2]
        /// 0 1 2
        /// > Write-Host $a[2..5]
        /// 2 3 4 5
        /// > Write-Host $a[0..($a.length-1)]
        /// 0 1 2 3 4 5
        ///
        /// </summary>
        public static T[] PwshRng<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len+1];
            for (int i = 0; i <= len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }
    }
}