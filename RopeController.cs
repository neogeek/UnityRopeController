using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ScottDoxey
{

    public class RopeController : MonoBehaviour
    {

        [Serializable]
        public struct SoftJointLimitSpringConfig
        {

            public float spring;

            public float damper;

            public static explicit operator SoftJointLimitSpring(SoftJointLimitSpringConfig a)
            {
                return new SoftJointLimitSpring { spring = a.spring, damper = a.damper };
            }

        }

        [Serializable]
        public struct SoftJointLimitConfig
        {

            public float limit;

            public float contactDistance;

            public static explicit operator SoftJointLimit(SoftJointLimitConfig a)
            {
                return new SoftJointLimit { limit = a.limit, contactDistance = a.contactDistance };
            }

        }

        [SerializeField]
        private int _length = 100;

        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private Rigidbody _anchor;

        [SerializeField]
        private Rigidbody _target;

        [SerializeField]
        private bool _generateCollidersForJoints;

        [SerializeField]
        private LayerMask _layerMaskForJoints = 1;

        [SerializeField]
        private Vector3 _axis = new(0, 1, 0);

        [SerializeField]
        private Vector3 _swingAxis = new(1, 0, 1);

        [SerializeField]
        private SoftJointLimitSpringConfig _twistLimitSpring = new() { spring = 1, damper = 1 };

        [SerializeField]
        private SoftJointLimitConfig _lowTwistLimit = new() { limit = -45, contactDistance = 5 };

        [SerializeField]
        private SoftJointLimitConfig _highTwistLimit = new() { limit = 45, contactDistance = 5 };

        [SerializeField]
        private SoftJointLimitSpringConfig _swingLimitSpring = new() { spring = 1, damper = 1 };

        [SerializeField]
        private SoftJointLimitConfig _swing1Limit = new() { limit = 140, contactDistance = 5 };

        [SerializeField]
        private SoftJointLimitConfig _swing2Limit = new() { limit = 140, contactDistance = 5 };

        private readonly List<Rigidbody> _joints = new();

        public ReadOnlyCollection<Rigidbody> Joints => _joints.AsReadOnly();

        private readonly List<GameObject> _spawned = new();

        public ReadOnlyCollection<GameObject> Spawned => _spawned.AsReadOnly();

        private void Start()
        {
            SetupRope();
        }

        private void Update()
        {
            UpdateRopeDraw();
        }

        public void SetupRope()
        {
            if (_anchor is null)
            {
                throw new InvalidOperationException($"The {nameof(_anchor)} component property was not set.");
            }

            if (_target is null)
            {
                throw new InvalidOperationException($"The {nameof(_target)} component property was not set.");
            }

            _anchor.isKinematic = true;
            _anchor.constraints = RigidbodyConstraints.FreezeAll;

            _joints.Add(_anchor);

            for (var i = 0; i < _length; i += 1)
            {
                var position = Vector3.Lerp(_anchor.transform.position, _target.transform.position,
                    Mathf.InverseLerp(0, _length, i));

                var rb = CreateRopeJoint(position, _joints[^1]);

                _joints.Add(rb);

                _spawned.Add(rb.gameObject);
            }

            SetupRopeJoint(_target.gameObject, _joints[^1]);

            _joints.Add(_target);

            _lineRenderer.positionCount = _joints.Count;
        }

        private Rigidbody CreateRopeJoint(Vector3 position, Rigidbody connectedBody)
        {
            var go = new GameObject
            {
                name = "Rope Joint (Spawned)",
                transform = { position = position },
                layer = (int)Mathf.Clamp(Mathf.Log(_layerMaskForJoints.value, 2), 0, 31)
            };

            var rb = go.AddComponent<Rigidbody>();

            rb.collisionDetectionMode = _anchor.collisionDetectionMode;

            if (_generateCollidersForJoints)
            {
                var jointCollider = go.AddComponent<SphereCollider>();

                jointCollider.radius = _lineRenderer.startWidth / 2;
            }

            SetupRopeJoint(rb.gameObject, connectedBody);

            return rb;
        }

        private void SetupRopeJoint(GameObject go, Rigidbody connectedBody)
        {
            var joint = go.AddComponent<CharacterJoint>();

            joint.axis = _axis;
            joint.swingAxis = _swingAxis;
            joint.twistLimitSpring = (SoftJointLimitSpring)_twistLimitSpring;
            joint.lowTwistLimit = (SoftJointLimit)_lowTwistLimit;
            joint.highTwistLimit = (SoftJointLimit)_highTwistLimit;
            joint.swingLimitSpring = (SoftJointLimitSpring)_swingLimitSpring;
            joint.swing1Limit = (SoftJointLimit)_swing1Limit;
            joint.swing2Limit = (SoftJointLimit)_swing2Limit;

            joint.connectedBody = connectedBody;
        }

        public void TeardownRope()
        {
            for (var i = 0; i < _spawned.Count; i += 1)
            {
                Destroy(_spawned[i]);
            }

            if (_target.TryGetComponent(typeof(CharacterJoint), out var joint))
            {
                Destroy(joint);
            }

            _joints.Clear();

            _spawned.Clear();

            _lineRenderer.positionCount = 0;
        }

        private void UpdateRopeDraw()
        {
            for (var i = 0; i < _joints.Count; i += 1)
            {
                _lineRenderer.SetPosition(i, _joints[i].position);
            }
        }

    }

}
