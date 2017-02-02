using UnityEngine;
using System.Collections;

public class ZombieHandAttack : MonoBehaviour
{
    private ZombieTracker ourZombie;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && ourZombie.state == ZombieTracker.ZombieState.Attacking)
        {
            StartCoroutine(attackDelay(other.gameObject));
        }
    }

    private IEnumerator attackDelay(GameObject player)
    {
        yield return new WaitForSeconds(0.5f);
        //player.GetComponent<PlayerEventsUI>().Death();
        yield return new WaitForSeconds(1.5f);
        ZombieManager.DestroyAllZombies();
    }
	// Use this for initialization
	void Start ()
    {
        ourZombie = GetComponentInParent<ZombieTracker>();
    }
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}
