# Homepage Sidecar
A Kubernetes Sidecar to auto-generate `service.yaml` configuration from Kubernetes Ingresses for Ben Phelps' [HomePage](https://github.com/benphelps/homepage).

## Roadmap
- [x] swappable kubeconfig (commmand line flag)
- [x] configurable output directory
- [ ] include manual config
- [x] ping
  - [x] fully qualify back end name
- [x] icon
  - [x] test image config
- [ ] homepage to support HTTP 204 as success
- [ ] enable services
  - [x] from annotation
  - [ ] default from config
- [ ] targets
  - [ ] from annotations
  - [x] default from config
- [ ] widgets
  - [x] basic
  - [x] secrets
    - [x] key
    - [ ] multi field