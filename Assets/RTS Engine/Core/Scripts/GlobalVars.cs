using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    public static class GlobalVars
    {
        public static int VERSION_YEAR => 2022;
        public static int VERSION_INITIAL => 3;
        public static int VERSION_SECONDARY => 1;

        public static bool IsNewerOrEqualVersion(int versionYear, int versionInitial, int versionSecondary)
            => versionYear >= VERSION_YEAR && versionInitial >= VERSION_INITIAL && versionSecondary >= VERSION_SECONDARY;
    }
}
