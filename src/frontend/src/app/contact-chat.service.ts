import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of, throwError } from 'rxjs';
import { environment } from '../environments/environment';

export type ContactChatMessage = {
  sender: 'visitor' | 'assistant' | 'owner' | 'system';
  content: string;
  sentAt: string;
};

export type ContactChatSession = {
  id: string;
  visitorName: string;
  createdAt: string;
  lastActivityAt: string;
  status: 'Active' | 'Waiting' | 'Closed' | 'Expired';
  queuePosition: number | null;
  messages: ContactChatMessage[];
};

export type ContactChatSummary = Omit<ContactChatSession, 'messages'> & {
  lastMessage: string;
};

@Injectable({ providedIn: 'root' })
export class ContactChatService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');
  private readonly credentials = { withCredentials: true };

  getVisitorSession(): Observable<ContactChatSession | null> {
    return this.http
      .get<ContactChatSession>(`${this.apiBaseUrl}/api/contact-chat/session`, this.credentials)
      .pipe(
        catchError(error =>
          error instanceof HttpErrorResponse && error.status === 404
            ? of(null)
            : throwError(() => error),
        ),
      );
  }

  sendVisitorMessage(content: string): Observable<ContactChatSession> {
    return this.http.post<ContactChatSession>(
      `${this.apiBaseUrl}/api/contact-chat/messages`,
      { content },
      this.credentials,
    );
  }

  listOwnerChats(): Observable<ContactChatSummary[]> {
    return this.http.get<ContactChatSummary[]>(
      `${this.apiBaseUrl}/api/admin/contact-chats`,
      this.credentials,
    );
  }

  getOwnerChat(id: string): Observable<ContactChatSession> {
    return this.http.get<ContactChatSession>(
      `${this.apiBaseUrl}/api/admin/contact-chats/${encodeURIComponent(id)}`,
      this.credentials,
    );
  }

  sendOwnerMessage(id: string, content: string): Observable<ContactChatSession> {
    return this.http.post<ContactChatSession>(
      `${this.apiBaseUrl}/api/admin/contact-chats/${encodeURIComponent(id)}/messages`,
      { content },
      this.credentials,
    );
  }

  closeOwnerChat(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiBaseUrl}/api/admin/contact-chats/${encodeURIComponent(id)}/close`,
      null,
      this.credentials,
    );
  }
}
