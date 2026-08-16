import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { SendMessageRequest, SendMessageResponse, WidgetConfig } from '../models/message.model';
import { ContactCaptureRequest, ContactCaptureResponse } from '../models/lead-capture.model';

@Injectable({
  providedIn: 'root',
})
export class ChatApiService {
  private http = inject(HttpClient);

  async fetchConfig(apiBaseUrl: string, businessId: string): Promise<WidgetConfig> {
    const url = `${apiBaseUrl.replace(/\/$/, '')}/api/v1/widget/${businessId}/config`;
    return firstValueFrom(this.http.get<WidgetConfig>(url));
  }

  async sendMessage(
    apiBaseUrl: string,
    businessId: string,
    payload: SendMessageRequest
  ): Promise<SendMessageResponse> {
    const url = `${apiBaseUrl.replace(/\/$/, '')}/api/v1/widget/${businessId}/chat`;
    return firstValueFrom(this.http.post<SendMessageResponse>(url, payload));
  }

  async submitContact(
    apiBaseUrl: string,
    businessId: string,
    payload: ContactCaptureRequest
  ): Promise<ContactCaptureResponse> {
    const url = `${apiBaseUrl.replace(/\/$/, '')}/api/v1/widget/${businessId}/contact`;
    return firstValueFrom(this.http.post<ContactCaptureResponse>(url, payload));
  }
}
