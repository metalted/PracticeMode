using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PracticeMode
{
    public class Shpleeble : MonoBehaviour
    {
        private SetupModelCar soapbox;
        private SetupModelCar cameraMan;
        private TextMeshPro displayName;
        private GameObject hornModel;
        private GameObject paragliderModel;
        private GameObject camera;
        private Transform armatureTop;       

        public void SetObjects(SetupModelCar soapbox, SetupModelCar cameraMan, TextMeshPro displayName, GameObject hornModel, GameObject paragliderModel, GameObject camera, Transform armatureTop)
        {
            this.soapbox = soapbox;
            this.cameraMan = cameraMan;
            this.displayName = displayName;
            this.hornModel = hornModel;
            this.paragliderModel = paragliderModel;
            this.camera = camera;
            this.armatureTop = armatureTop;

            cameraMan.gameObject.SetActive(false);
            soapbox.gameObject.SetActive(true);
            paragliderModel.gameObject.SetActive(false);
            displayName.gameObject.SetActive(false);
        }

        public void SetCosmetics(CosmeticsV16 cosmetics)
        {
            soapbox.DoCarSetup(cosmetics, false, false, true);
        }
    }
}
