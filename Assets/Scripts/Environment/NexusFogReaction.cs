using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NexusFogReaction : MonoBehaviour
{
    #region Variáveis
    // VARIÁVEIS VISÍVEIS
    [Header("Referências")]
        [SerializeField]
    private Transform _player;
        [SerializeField]
    private ParticleSystem[] _fogSystems;

    [Header("Reação")]
        [SerializeField, Min(0.1f), Tooltip("Raio de influência ao redor do jogador")]
    private float _radius = 3.2f;
        [SerializeField, Min(0f), Tooltip("Força radial que afasta as partículas")]
    private float _pushStrength = 2.4f;
        [SerializeField, Min(0f), Tooltip("Força de redemoinho ao redor do jogador")]
    private float _swirlStrength = 1.2f;
        [SerializeField, Min(0f), Tooltip("Empurrão vertical suave")]
    private float _upLift = 0.25f;
        [SerializeField, Min(0.1f), Tooltip("Velocidade máxima aplicada às partículas")]
    private float _maxVelocity = 2.5f;
        [SerializeField, Min(0f), Tooltip("Velocidade mínima do jogador para a névoa reagir")]
    private float _minPlayerSpeed = 0.4f;

    // VARIÁVEIS OCULTAS
    private readonly Dictionary<ParticleSystem, ParticleSystem.Particle[]> _buffers =
        new Dictionary<ParticleSystem, ParticleSystem.Particle[]>();

    private Vector3 _lastPlayerPosition;
    private bool _ready;

    #endregion

    #region Funções Unity
    private void Start()
    {
        if (_player == null)
        {
            GameObject i_player = GameObject.FindGameObjectWithTag("Player");
            if (i_player != null)
                _player = i_player.transform;
        }

        if (_fogSystems == null || _fogSystems.Length == 0)
            FindFogSystems();

        if (_player != null)
            _lastPlayerPosition = _player.position;

        _ready = _player != null && _fogSystems != null && _fogSystems.Length > 0;
    }

    private void LateUpdate()
    {
        if (!_ready || _player == null || Time.deltaTime <= 0f)
            return;

        Vector3 i_playerVelocity = (_player.position - _lastPlayerPosition) / Time.deltaTime;
        _lastPlayerPosition = _player.position;

        if (i_playerVelocity.magnitude < _minPlayerSpeed)
            return;

        for (int i = 0; i < _fogSystems.Length; i++)
            React(_fogSystems[i], i_playerVelocity);
    }

    #endregion

    #region Utilidades
    private void React(ParticleSystem i_system, Vector3 i_playerVelocity)
    {
        if (i_system == null || !i_system.isPlaying)
            return;

        int i_count = i_system.particleCount;

        if (i_count <= 0)
            return;

        if (!_buffers.TryGetValue(i_system, out ParticleSystem.Particle[] i_particles) ||
            i_particles.Length < i_count)
        {
            i_particles = new ParticleSystem.Particle[Mathf.Max(i_count, 64)];
            _buffers[i_system] = i_particles;
        }

        i_count = i_system.GetParticles(i_particles);

        ParticleSystem.MainModule i_main = i_system.main;
        bool i_worldSpace = i_main.simulationSpace == ParticleSystemSimulationSpace.World;

        Vector3 i_center = i_worldSpace
            ? _player.position
            : i_system.transform.InverseTransformPoint(_player.position);

        Vector3 i_velocity = i_worldSpace
            ? i_playerVelocity
            : i_system.transform.InverseTransformVector(i_playerVelocity);

        Vector3 i_up = i_worldSpace
            ? Vector3.up
            : i_system.transform.InverseTransformDirection(Vector3.up);

        float i_dt = Time.deltaTime;

        for (int i = 0; i < i_count; i++)
        {
            Vector3 i_delta = i_particles[i].position - i_center;
            float i_distance = i_delta.magnitude;

            if (i_distance > _radius || i_distance < 0.001f)
                continue;

            float i_weight = 1f - (i_distance / _radius);
            Vector3 i_radial = i_delta / i_distance;
            Vector3 i_swirl = Vector3.Cross(i_up, i_radial);

            Vector3 i_force =
                (i_radial * _pushStrength +
                 i_swirl * _swirlStrength +
                 i_up * _upLift) * (i_weight * i_dt);

            i_force += i_velocity * (i_weight * 0.12f * i_dt);

            i_particles[i].velocity += i_force;

            if (i_particles[i].velocity.magnitude > _maxVelocity)
                i_particles[i].velocity = i_particles[i].velocity.normalized * _maxVelocity;
        }

        i_system.SetParticles(i_particles, i_count);
    }

    [ContextMenu("Encontrar sistemas de névoa")]
    public void FindFogSystems()
    {
        List<ParticleSystem> i_found = new List<ParticleSystem>();

        ParticleSystem[] i_children = GetComponentsInChildren<ParticleSystem>(true);

        if (i_children.Length > 0)
            i_found.AddRange(i_children);

        if (i_found.Count == 0)
        {
            ParticleSystem[] i_all = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

            foreach (ParticleSystem i_system in i_all)
            {
                string i_name = i_system.gameObject.name.ToLowerInvariant();
                Transform i_parent = i_system.transform.parent;

                if (i_parent != null)
                    i_name += " " + i_parent.name.ToLowerInvariant();

                if (i_name.Contains("fog") || i_name.Contains("névoa") || i_name.Contains("nevoa"))
                    i_found.Add(i_system);
            }
        }

        _fogSystems = i_found.ToArray();
    }
    #endregion
}
