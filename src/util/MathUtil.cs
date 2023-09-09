// Copyright (C) 2023 Karl Pickett / ViLark Project
namespace vilark;

class MathUtil {
    static public int GetPercent(int n, int total) {
        if (total == 0) {
            return 100;
        } else {
            int ret = (n * 100 / total);
            ret = Math.Clamp(ret, 0, 100);
            return ret;
        }
    }
}
