import { RenderMode, ServerRoute } from '@angular/ssr';
import {AppComponent} from './app.component';

export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Client },
  {path: 'user-session', renderMode: RenderMode.Server},
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }


];
