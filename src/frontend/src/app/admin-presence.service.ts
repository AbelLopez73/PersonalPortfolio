import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
} from '@microsoft/signalr';
import { environment } from '../environments/environment';

type AdminSessionResponse = {
  loginEnabled: boolean;
  isAdmin: boolean;
  displayName: string | null;
  isConnected: boolean;
};

export type AdminPresenceState =
  | 'checking'
  | 'unavailable'
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';

@Injectable({ providedIn: 'root' })
export class AdminPresenceService {
  readonly state = signal<AdminPresenceState>('checking');
  readonly isAdmin = signal(false);
  readonly loginEnabled = signal(false);
  readonly displayName = signal<string | null>(null);

  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');
  private connection?: HubConnection;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      void this.stopConnection();
    });
  }

  get loginUrl(): string {
    return `${this.apiBaseUrl}/api/admin/login`;
  }

  async initialize(): Promise<void> {
    if (this.state() !== 'checking') {
      return;
    }

    try {
      const session = await firstValueFrom(
        this.http.get<AdminSessionResponse>(`${this.apiBaseUrl}/api/admin/session`, {
          withCredentials: true,
        }),
      );

      this.loginEnabled.set(session.loginEnabled);
      this.isAdmin.set(session.isAdmin);
      this.displayName.set(session.displayName);

      if (!session.isAdmin) {
        this.state.set(session.loginEnabled ? 'disconnected' : 'unavailable');
        return;
      }

      this.state.set(session.isConnected ? 'connected' : 'connecting');
      await this.startConnection();
    } catch {
      this.state.set('unavailable');
    }
  }

  async logout(): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBaseUrl}/api/admin/logout`, null, {
        withCredentials: true,
      }),
    );
    await this.stopConnection();
    this.isAdmin.set(false);
    this.displayName.set(null);
    this.state.set(this.loginEnabled() ? 'disconnected' : 'unavailable');
  }

  private async startConnection(): Promise<void> {
    const connection = new HubConnectionBuilder()
      .withUrl(`${this.apiBaseUrl}/hubs/admin`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.onreconnecting(() => this.state.set('reconnecting'));
    connection.onreconnected(() => this.state.set('connected'));
    connection.onclose(() => this.state.set('disconnected'));
    this.connection = connection;

    try {
      await connection.start();
      this.state.set('connected');
    } catch {
      this.state.set('disconnected');
    }
  }

  private async stopConnection(): Promise<void> {
    const connection = this.connection;
    this.connection = undefined;

    if (connection) {
      await connection.stop();
    }
  }
}