using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class fxController : MonoBehaviour {

    public int color = 0;
    public int FX = 0;
    public GameObject[] fxObject;
    public Text colorTxt;
    public Text FXTxt;
    public Camera myCamera;
    int cameraZoom = 1;
    float[] cameraFOV = new float[] { 25, 35, 45 };

    void Start() {
        for (int i = 0; i < 4; i++) {
            fxObject[i].SetActive(false);
        }
        SetColorTxt(color, FX);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            color++;
            color = Mathf.Clamp(color, 0, 3);
            DisabledFX();
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            color--;
            color = Mathf.Clamp(color, 0, 3);
            DisabledFX();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            FX--;
            FX = Mathf.Clamp(FX, 0, 9);
            DisabledFX();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            FX++;
            FX = Mathf.Clamp(FX, 0, 9);
            DisabledFX();
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus)) {
            cameraZoom = Mathf.Clamp(cameraZoom + 1, 0, 2);
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus)) {
            cameraZoom = Mathf.Clamp(cameraZoom - 1, 0, 2);
        }
        myCamera.fieldOfView 
            = Mathf.Lerp(myCamera.fieldOfView, cameraFOV[cameraZoom], 0.2f);

        switch (FX) {
            case 0:
                if (color == 0) { fxObject[0].SetActive(true); };
                if (color == 1) { fxObject[1].SetActive(true); };
                if (color == 2) { fxObject[2].SetActive(true); };
                if (color == 3) { fxObject[3].SetActive(true); };
                break;
            case 1:
                if (color == 0) { fxObject[4].SetActive(true); };
                if (color == 1) { fxObject[5].SetActive(true); };
                if (color == 2) { fxObject[6].SetActive(true); };
                if (color == 3) { fxObject[7].SetActive(true); };
                break;
            case 2:
                if (color == 0) { fxObject[8].SetActive(true); };
                if (color == 1) { fxObject[9].SetActive(true); };
                if (color == 2) { fxObject[10].SetActive(true); };
                if (color == 3) { fxObject[11].SetActive(true); };
                break;
            case 3:
                if (color == 0) { fxObject[12].SetActive(true); };
                if (color == 1) { fxObject[13].SetActive(true); };
                if (color == 2) { fxObject[14].SetActive(true); };
                if (color == 3) { fxObject[15].SetActive(true); };
                break;
            case 4:
                if (color == 0) { fxObject[16].SetActive(true); };
                if (color == 1) { fxObject[17].SetActive(true); };
                if (color == 2) { fxObject[18].SetActive(true); };
                if (color == 3) { fxObject[19].SetActive(true); };
                break;
            case 5:
                if (color == 0) { fxObject[20].SetActive(true); };
                if (color == 1) { fxObject[21].SetActive(true); };
                if (color == 2) { fxObject[22].SetActive(true); };
                if (color == 3) { fxObject[23].SetActive(true); };
                break;
            case 6:
                if (color == 0) { fxObject[24].SetActive(true); };
                if (color == 1) { fxObject[25].SetActive(true); };
                if (color == 2) { fxObject[26].SetActive(true); };
                if (color == 3) { fxObject[27].SetActive(true); };
                break;
            case 7:
                if (color == 0) { fxObject[28].SetActive(true); };
                if (color == 1) { fxObject[29].SetActive(true); };
                if (color == 2) { fxObject[30].SetActive(true); };
                if (color == 3) { fxObject[31].SetActive(true); };
                break;
            case 8:
                if (color == 0) { fxObject[32].SetActive(true); };
                if (color == 1) { fxObject[33].SetActive(true); };
                if (color == 2) { fxObject[34].SetActive(true); };
                if (color == 3) { fxObject[35].SetActive(true); };
                break;
            case 9:
                if (color == 0) { fxObject[36].SetActive(true); };
                if (color == 1) { fxObject[37].SetActive(true); };
                if (color == 2) { fxObject[38].SetActive(true); };
                if (color == 3) { fxObject[39].SetActive(true); };
                break;
        }
    }

    void DisabledFX() {
        for (int i = 0; i < 40; i++) {
            fxObject[i].SetActive(false);
        }
        SetColorTxt(color,FX);
    }

    void SetColorTxt(int i, int j) {
        i++;
        j++;
        colorTxt.text = "Color   : " + i.ToString() + " / 4";
        FXTxt.text = "Portal  : " + j.ToString() + " / 10";
    }
}