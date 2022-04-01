using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Gismo.Quip.Minerals;
namespace Gismo.Quip
{ 
    public class PlayerCentralization : Singleton<PlayerCentralization>
    {

        [HideInInspector] public List<Mineral> pickedUpMineral;

        [SerializeField] private TextMeshProUGUI mineralPickupCounter;

        public override void Awake()
        {
            base.Awake();
            Init();
        }

        public void Init()
        {
            pickedUpMineral = new List<Mineral>();
            UpdateUI();
        }

        public void UpdateUI()
        {
            mineralPickupCounter.text = $"{pickedUpMineral.Count} minerals";
        }

        public void PickupMineral(Mineral newMineral)
        {
            pickedUpMineral.Add(newMineral);
            UpdateUI();
        }
    }
}
