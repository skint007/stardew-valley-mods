using System;
using LevelUp.Config;

namespace LevelUp.Systems;

/// <summary>
/// Pure math for the XP curve. No side effects, no SMAPI deps — safe to unit test.
///
/// Curve formula:
///     xpToNext(N) = floor(baseXp * growthRate^(N-1))
///
/// where xpToNext(N) is the XP required to go from level N → N+1.
/// "TotalXp at level L" = sum of xpToNext(1..L-1).
/// </summary>
public class LevelCalculator
{
    private readonly CurveConfig _curve;
    private readonly int _levelCap;

    // Cached cumulative XP thresholds: _cumulativeXp[L] = total XP required to reach level L.
    // _cumulativeXp[1] = 0, _cumulativeXp[2] = xpToNext(1), etc.
    private long[] _cumulativeXp;

    public LevelCalculator(CurveConfig curve, int levelCap)
    {
        _curve = curve;
        _levelCap = Math.Max(1, levelCap);
        _cumulativeXp = Array.Empty<long>();
        Rebuild();
    }

    /// <summary>
    /// Recompute the cached threshold table. Call when curve params or level cap change.
    /// </summary>
    public void Rebuild()
    {
        var (baseXp, growth) = _curve.Resolve();
        _cumulativeXp = new long[_levelCap + 1];
        _cumulativeXp[1] = 0;

        double current = baseXp;
        long running = 0;
        for (int level = 2; level <= _levelCap; level++)
        {
            running += (long)Math.Floor(current);
            _cumulativeXp[level] = running;
            current *= growth;
        }
    }

    /// <summary>Total XP needed to reach the given level from level 1.</summary>
    public long CumulativeXpForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level > _levelCap) return _cumulativeXp[_levelCap];
        return _cumulativeXp[level];
    }

    /// <summary>XP required to go from level N → N+1.</summary>
    public long XpToNextLevel(int level)
    {
        if (level >= _levelCap) return 0;
        return _cumulativeXp[level + 1] - _cumulativeXp[level];
    }

    /// <summary>Given total lifetime XP, return the resulting level (capped).</summary>
    public int LevelForTotalXp(long totalXp)
    {
        if (totalXp <= 0) return 1;
        // Binary search for the largest level whose cumulative threshold <= totalXp.
        int lo = 1;
        int hi = _levelCap;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (_cumulativeXp[mid] <= totalXp) lo = mid;
            else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>XP earned into the current level (0 .. XpToNextLevel(level)).</summary>
    public long XpIntoCurrentLevel(long totalXp, int level)
    {
        if (level >= _levelCap) return 0;
        return totalXp - CumulativeXpForLevel(level);
    }

    /// <summary>0.0 .. 1.0 progress toward next level (or 1.0 if at cap).</summary>
    public float ProgressToNext(long totalXp, int level)
    {
        if (level >= _levelCap) return 1f;
        long needed = XpToNextLevel(level);
        if (needed <= 0) return 1f;
        long into = XpIntoCurrentLevel(totalXp, level);
        return Math.Clamp((float)into / needed, 0f, 1f);
    }

    public int LevelCap => _levelCap;
}
