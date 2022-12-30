using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamSelector : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs;

    private GameObject beam;

    public void CreateBeam(int index)
    {
        if (beam)
        {
            Destroy(beam);
        }

        beam = Instantiate(prefabs[index]);
    }

    public void Show()
    {
        if(beam)
        {
            beam.GetComponent<Animator>().Play("Show");
        }
    }

    public void Hide()
    {
        if(beam)
        {
            beam.GetComponent<Animator>().Play("Hide");
        }
    }
}
