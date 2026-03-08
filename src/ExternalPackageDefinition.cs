// Author: František Holubec
// Created: 08.03.2026

using UnityEngine;

namespace ExternalPackages
{
    public class ExternalPackageDefinition : ScriptableObject
    {
        [SerializeField]
        private string _Assembly;
        
        [SerializeField]
        private string _Define;
        
        public string Assembly => _Assembly;
        public string Define => _Define;    
    }
}
