using System;
using UnityEngine;

namespace KnightLife.Runtime.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Destroying Network Manager");
                Destroy(gameObject);
            }
        }
    }
}
