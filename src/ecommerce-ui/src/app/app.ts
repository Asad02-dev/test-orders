import { Component } from '@angular/core';
import { LayoutComponent } from './shared/components/layout/layout';
import { ToastComponent } from './shared/components/toast/toast';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [LayoutComponent, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
