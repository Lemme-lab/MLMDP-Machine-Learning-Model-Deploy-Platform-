export class Deployment {
  constructor(
    public name: string,
    public namespace: string,
    public replicas: number,
    public availableReplicas: number,
    public readyReplicas: number,
    public creationTimestamp: string,
    public labels: { [key: string]: string },
    public annotations: { [key: string]: string },
    public selector: { [key: string]: string },
    public strategy: string,
    public minReadySeconds: number | null,
    public revisionHistoryLimit: number,
    public conditions: Array<Condition>,
    public podTemplate: Array<PodTemplate>,
    public volumes: Array<Volume>,
    public ownerReferences: Array<OwnerReference> | null,
    public service: Service | null
  ) {}
}

export class Condition {
  constructor(
    public type: string,
    public status: string,
    public lastTransitionTime: string
  ) {}
}

export class PodTemplate {
  constructor(
    public containerName: string,
    public image: string,
    public ports: Array<ContainerPort>,
    public resources: Resources | null, // Resources can be defined as a type or null
    public env: Array<EnvironmentVariable> | null,
    public imagePullPolicy: string
  ) {}
}

export class ContainerPort {
  constructor(
    public containerPort: number,
    public protocol: string
  ) {}
}

export class Volume {
  constructor(
    public name: string,
    public volumeType: string,
    public claimName: string
  ) {}
}

export class OwnerReference {
  constructor(
    public apiVersion: string,
    public kind: string,
    public name: string,
    public uid: string
  ) {}
}

export class Service {
  constructor(
    public clusterIP: string | null,
    public ports: Array<ServicePort>,
    public error: string | null
  ) {}
}

export class ServicePort {
  constructor(
    public port: number,
    public targetPort: TargetPort,
    public protocol: string
  ) {}
}

export class TargetPort {
  constructor(
    public value: string | number
  ) {}
}

export class Resources {
  constructor(
    public requests: any | null, // Adjust types based on your use case
    public limits: any | null
  ) {}
}

export class EnvironmentVariable {
  constructor(
    public name: string,
    public value: string
  ) {}
}
