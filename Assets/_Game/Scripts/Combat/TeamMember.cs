using UnityEngine;

namespace PixelBrawl.Combat
{
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private Team team;

        public Team Team => team;
    }
}
