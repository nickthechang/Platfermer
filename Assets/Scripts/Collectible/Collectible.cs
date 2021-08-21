using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    /*private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Hit");
        if (collision.tag == "Player")
        {
            collision.GetComponent<Controller2D>().PossessKey();
            Debug.Log(collision.GetComponent<Controller2D>().KeyStatus());
            gameObject.SetActive(false);
        }
    }*/

    public void EatMyAss()
    {
        gameObject.SetActive(false);
    }
}
