using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCCountDisplayer : MonoBehaviour
{
    public Slider NPCCountSlider;
    public void UpdateNPCCount()
    {
        gameObject.transform.GetComponent<TMP_Text>().text = NPCCountSlider.GetComponent<Slider>().value.ToString();
    }
}
