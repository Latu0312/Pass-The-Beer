using System.Collections.Generic;
using UnityEngine;


public static class OrderDatabase
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (orders.Count == 0) orders.AddRange(new string[] { "beer", "wine", "soda", "tea", "frappe", "cocktail", });
    }


    private static List<string> orders = new List<string>();


    public static string GetRandomOrderId() => orders[Random.Range(0, orders.Count)];
}