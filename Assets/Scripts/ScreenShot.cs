using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
	private int count = 0;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1)){
			ScreenCapture.CaptureScreenshot(count.ToString() + ".png");
			count++;
		}
    }
}
