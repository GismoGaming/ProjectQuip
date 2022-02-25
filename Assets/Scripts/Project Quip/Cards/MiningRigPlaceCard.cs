using System.Collections.Generic;
using UnityEngine;
using Gismo.Quip.Mineral;

namespace Gismo.Quip.Cards
{
    public class MiningRigPlaceCard : CardDropBase
    {
        [SerializeField] private float checkRadius;

        [SerializeField] private List<MineralDeposit> mineralDeposits;

        public override void Awake()
        {
            mineralDeposits = new List<MineralDeposit>(FindObjectsOfType<MineralDeposit>());

            OnCardDown += TryPlaceDownMiningRig;
            base.Awake();
        }

        void TryPlaceDownMiningRig(Vector3 position)
        {
            Debug.Log(position);
            foreach(MineralDeposit deposit in mineralDeposits)
            {
                if(Vector3.Distance(position,deposit.transform.position) <= checkRadius && deposit.CanPlaceMiner())
                {
                    deposit.StartDepositOutput();

                    return;
                }
            }
        }
    }
}

