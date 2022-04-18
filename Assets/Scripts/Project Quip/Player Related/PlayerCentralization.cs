using System.Collections.Generic;
using UnityEngine;
using TMPro;

using Gismo.Quip.Minerals;
namespace Gismo.Quip
{
    [System.Serializable]
    public class SpecialCardElements
    {
        public GameObject prefab;
        public int maxOut;
        public float cooldown;

        public int maxCarry;
    }

    public class PlayerCentralization : Singleton<PlayerCentralization>
    {
        int amountCarried;
        private List<MineralCostDetails> pickedUpMineralDetails;
        private List<uint> pickedUpMineralIDs;

        public Tools.VisualDictionary<Role, SpecialCardElements> roleSpecialsTable;

        [SerializeField] private TextMeshProUGUI mineralPickupCounter;

        public Role playerRole;

        private bool isCarryingCompressed;

        public override void Awake()
        {
            base.Awake();
            Init();
        }

        public void Init()
        {
            pickedUpMineralDetails = new List<MineralCostDetails>();
            pickedUpMineralIDs = new List<uint>();
            UpdateUI();
        }

        public void UpdateUI()
        {
            mineralPickupCounter.text = $"{pickedUpMineralDetails.Count} minerals";
        }

        public void PickupMineral(Mineral newMineral)
        {
            pickedUpMineralDetails.Add(MineralDatabase.Instance.GetCostDetails(newMineral.GetMineralType()) );
            pickedUpMineralIDs.Add(newMineral.ID);

            amountCarried++;
            UpdateUI();
        }

        public void PickupCompressedCube(CompressedMineralCube cube)
        {
            amountCarried = roleSpecialsTable[playerRole].maxCarry;

            pickedUpMineralDetails.Add(cube.GetMineralCosts());
            pickedUpMineralIDs.Add(cube.ID);

            isCarryingCompressed = true;

            UpdateUI();
        }

        public bool HasMineralsInHand()
        {
            return amountCarried > 0;
        }

        public bool CanDropOffToCompressed()
        {
            if (playerRole == Role.Hauler)
            {
                return !isCarryingCompressed && HasMineralsInHand();
            }
            return HasMineralsInHand();
        }

        public bool CanPickup(bool isCompressed)
        {
            if(isCompressed)
            {
                return amountCarried == 0;
            }
            else
            {
                return amountCarried < roleSpecialsTable[playerRole].maxCarry;
            }
        }

        public List<MineralCostDetails> GetListOfPickedUpMineralsCostDetails()
        {
            return pickedUpMineralDetails;
        }

        public List<uint> GetListOfPickedUpMineralIDs()
        {
            return pickedUpMineralIDs;
        }

        public void ClearPickedUpMinerals()
        {
            amountCarried = 0;
            pickedUpMineralIDs.Clear();
            pickedUpMineralDetails.Clear();

            if(playerRole == Role.Hauler)
            {
                isCarryingCompressed = false;
            }

            UpdateUI();
        }
    }
}
