using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URandom = UnityEngine.Random;

public class Utils
{
    private static readonly NormalItem.eNormalType[] _allTypes = (NormalItem.eNormalType[])Enum.GetValues(typeof(NormalItem.eNormalType));

    public static NormalItem.eNormalType GetRandomNormalType()
    {
        return _allTypes[URandom.Range(0, _allTypes.Length)];
    }

    public static NormalItem.eNormalType GetRandomNormalTypeExcept(NormalItem.eNormalType[] types)
    {
        // Avoid LINQ Except() and ToList()
        // We know _allTypes is small (7 elements), so a simple array or list without LINQ is better.
        List<NormalItem.eNormalType> list = new List<NormalItem.eNormalType>(_allTypes.Length);
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
                list.Add(_allTypes[i]);
            }
        }

        int rnd = URandom.Range(0, list.Count);
        return list[rnd];
    }
}
