import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PopupWindowService {
  private popup: Window | null = null;

  open(): boolean {
    if (this.popup && !this.popup.closed) {
      this.popup.focus();
      return true;
    }

    const url = `${window.location.origin}/popup`;
    const features = [
      'popup=yes',
      'width=320',
      'height=440',
      'left=48',
      'top=48',
      'resizable=yes',
      'scrollbars=no',
      'menubar=no',
      'toolbar=no',
      'location=no',
      'status=no'
    ].join(',');

    this.popup = window.open(url, 'corelens-popup', features);
    return this.popup != null;
  }
}
