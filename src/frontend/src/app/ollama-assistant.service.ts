import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export type AssistantMessage = {
  role: 'user' | 'assistant';
  content: string;
};

type AssistantChatResponse = {
  reply: string;
  contactChatStarted: boolean;
  contactChatId: string | null;
  conversationId: string;
};

@Injectable({ providedIn: 'root' })
export class OllamaAssistantService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  chat(messages: AssistantMessage[], conversationId: string): Observable<AssistantChatResponse> {
    return this.http.post<AssistantChatResponse>(
      `${this.apiBaseUrl}/api/assistant/chat`,
      { messages, conversationId },
      { withCredentials: true },
    );
  }
}