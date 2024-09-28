export class Pod {
  private _podServiceIp: string;
  private _podServiceIpPort: string;
  private _podName: string;
  private _status: string;
  private _specContainers: string;
  private _statusMessage: string;
  private _logs: string; // Add logs property

  constructor(podServiceIp: string, podServiceIpPort: string, podName: string, status: string, specContainers: string, statusMessage: string, logs: string) {
    this._podServiceIp = podServiceIp;
    this._podServiceIpPort = podServiceIpPort;
    this._podName = podName;
    this._status = status;
    this._specContainers = specContainers;
    this._statusMessage = statusMessage;
    this._logs = logs; // Initialize logs
  }

  get podServiceIp(): string {
    return this._podServiceIp;
  }

  set podServiceIp(value: string) {
    this._podServiceIp = value;
  }

  get podServiceIpPort(): string {
    return this._podServiceIpPort;
  }

  set podServiceIpPort(value: string) {
    this._podServiceIpPort = value;
  }

  get podName(): string {
    return this._podName;
  }

  set podName(value: string) {
    this._podName = value;
  }

  get status(): string {
    return this._status;
  }

  set status(value: string) {
    this._status = value;
  }

  get specContainers(): string {
    return this._specContainers;
  }

  set specContainers(value: string) {
    this._specContainers = value;
  }

  get statusMessage(): string {
    return this._statusMessage;
  }

  set statusMessage(value: string) {
    this._statusMessage = value;
  }

  get logs(): string { // Getter for logs
    return this._logs;
  }

  set logs(value: string) { // Setter for logs
    this._logs = value;
  }
}
