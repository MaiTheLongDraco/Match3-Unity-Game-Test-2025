using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URandom = UnityEngine.Random;

public class Utils
{
    private static readonly NormalItem.eNormalType[] _allTypes = (NormalItem.eNormalType[])Enum.GetValues(typeof(NormalItem.eNormalType));

    // Buffer tái sử dụng cho hàm lọc (Zero GC)
    private static readonly NormalItem.eNormalType[] _candidateBuffer = new NormalItem.eNormalType[7];

    public static NormalItem.eNormalType GetRandomNormalType()
    {
        return _allTypes[URandom.Range(0, _allTypes.Length)];
    }

    /// <summary>
    /// Overload nhận List thay vì Array — tránh .ToArray() gây GC allocation
    /// </summary>
    public static NormalItem.eNormalType GetRandomNormalTypeExcept(List<NormalItem.eNormalType> excludeTypes)
    {
        int count = 0;
        for (int i = 0; i < _allTypes.Length; i++)
        {
            bool excluded = false;
            if (excludeTypes != null)
            {
                for (int j = 0; j < excludeTypes.Count; j++)
                {
                    if (_allTypes[i] == excludeTypes[j])
                    {
                        excluded = true;
                        break;
                    }
                }
            }
            if (!excluded)
            {
                _candidateBuffer[count++] = _allTypes[i];
            }
        }

        if (count == 0) return _allTypes[URandom.Range(0, _allTypes.Length)];
        return _candidateBuffer[URandom.Range(0, count)];
    }

    /// <summary>
    /// Backward-compatible overload cho code cũ
    /// </summary>
    public static NormalItem.eNormalType GetRandomNormalTypeExcept(NormalItem.eNormalType[] types)
    {
        int count = 0;
        for (int i = 0; i < _allTypes.Length; i++)
        {
            bool excluded = false;
            if (types != null)
            {
                for (int j = 0; j < types.Length; j++)
                {
                    if (_allTypes[i] == types[j])
                    {
                        excluded = true;
                        break;
                    }
                }
            }
            if (!excluded)
            {
                _candidateBuffer[count++] = _allTypes[i];
            }
        }

        if (count == 0) return _allTypes[URandom.Range(0, _allTypes.Length)];
        return _candidateBuffer[URandom.Range(0, count)];
    }
}
