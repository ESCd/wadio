import Leaflet, { MapOptions as BaseMapOptions, CircleMarker, LatLng, LatLngBounds, LatLngExpression, Marker } from 'leaflet';
import { LocateControl } from 'leaflet.locatecontrol';
import 'leaflet.markercluster';

import { animate, debounce } from './core';

Leaflet.Icon.Default.imagePath = '/Interop/map/';

const DefaultMarkerOptions: Leaflet.CircleMarkerOptions = (() => {
  const color = window.getComputedStyle(document.documentElement).getPropertyValue('--color-accent');
  return Object.seal({
    color: color,
    fillColor: color,
    fillOpacity: 0.6,
  });
})();

export function createMap(element: HTMLElement, options: MapOptions, events: MapEvents) {
  const location = options.enableLocate ? new LocateControl({
    cacheLocation: true,
    clickBehavior: {
      inView: 'setView',
      inViewNotFollowing: 'inView',
      outOfView: 'setView',
    },
    flyTo: true,
    keepCurrentZoomLevel: true,
    locateOptions: {
      enableHighAccuracy: true,
      watch: false,
    },
    showPopup: false,
    showCompass: false,
    strings: {
      title: 'Current Location',
    },

    onLocationError() { },
  }) : undefined;

  const map = Leaflet.map(element, {
    ...options,
    maxBounds: [[180, -Infinity], [-180, Infinity]],
    maxBoundsViscosity: 1,
    preferCanvas: true,
  }).addLayer(Leaflet.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    crossOrigin: true,
    keepBuffer: options.keepBuffer,
    maxZoom: options.maxZoom,
  }));

  location?.addTo(map);
  map.whenReady(() => new Promise<BoundsChangeTracker>(resolve => {
    const tracker = new BoundsChangeTracker(map);
    if (location) {
      map.once('locationfound', () => map.once('moveend', () => resolve(tracker)));
      map.once('locationerror', () => resolve(tracker));

      return location.start();
    }

    return resolve(tracker);
  }).then(tracker => {
    const handler = debounce(() => window.requestAnimationFrame(() => {
      if (tracker.refresh()) {
        return events.invokeMethodAsync('OnBoundsChanged', tracker.get());
      }
    }), 25);

    map.on('moveend', handler);
    element.addEventListener('resize', handler, {
      passive: true
    });

    return events.invokeMethodAsync('OnReady', tracker.get());
  }));

  const markers = Leaflet.markerClusterGroup({
    chunkedLoading: true,
    showCoverageOnHover: false,
    spiderLegPolylineOptions: {
      ...DefaultMarkerOptions
    }
  }).addTo(map);

  const pool: CircleMarker[] = [];
  const createMarker = (options: MarkerOptions) => {
    const marker = pool.pop();
    if (marker) {
      if (marker.getLatLng().equals(options.position)) {
        return marker;
      }

      return marker.setLatLng(options.position);
    }

    return Leaflet.circleMarker(options.position, DefaultMarkerOptions);
  };

  const addMarker = async (marker: CircleMarker, options: MarkerOptions, events: MarkerEvents) => {
    await animate(() => marker.addTo(markers));

    const popup = Leaflet.popup({
      className: [options.style === MarkerStyle.Custom ? 'custom' : 'loading'].join(' '),
      closeButton: options.style === MarkerStyle.Custom ? false : options.closeButton,
      content: options.title ?? 'Loading...',
    });

    marker.bindPopup(popup)
      .on('popupclose', async () => {
        await events.invokeMethodAsync('OnPopupClosed');
        await animate(() => popup.getElement()?.classList.add('loading'));
      }).on('popupopen', async () => {
        await events.invokeMethodAsync('OnPopupOpen');
        await animate(() => popup.getElement()?.classList.remove('loading'));
      });

    if (options.autoPopup) {
      return animate(() => marker.openPopup());
    }
  };

  return {
    async addMarker(options: MarkerOptions, events: MarkerEvents) {
      const marker = createMarker(options);
      await addMarker(
        marker,
        options,
        events);

      return {
        dispose() {
          this.reset();
          pool.push(marker);
        },

        reset() {
          markers.removeLayer(marker);

          marker.off();
          marker.unbindPopup();
        },

        setPopupContent(element: HTMLElement) {
          const popup = marker.getPopup();
          if (popup) {
            return animate(() => {
              popup.getElement()?.classList.remove('loading');
              popup.setContent(element).update();
            });
          }
        },

        update(options: MarkerOptions, events: MarkerEvents) {
          marker.setLatLng(options.position);
          return addMarker(marker, options, events);
        }
      }
    },

    dispose() {
      pool.splice(0, pool.length);

      map.remove();
      map.off();
    },

    async refresh() {
      await animate(() => map.invalidateSize({
        debounceMoveend: true,
        pan: true,
      }));

      if (!options.dragging && options.center) {
        if (!map.getCenter().equals(options.center)) {
          return animate(() => map.flyTo(options.center!));
        }
      }
    },
  };
}

class BoundsChangeTracker {
  private static readonly PADDING = 0.0333;

  private readonly map: Leaflet.Map;
  private value: OnBoundsChangedEvent;

  constructor(map: Leaflet.Map) {
    this.map = map;
    this.value = BoundsChangeTracker.createEvent(map.getBounds().pad(BoundsChangeTracker.PADDING));
  }

  private static createEvent(bounds: LatLngBounds): OnBoundsChangedEvent {
    const center = BoundsChangeTracker.normalizeCoord(bounds.getCenter());
    const corner = BoundsChangeTracker.normalizeCoord(bounds.getNorthEast());

    return {
      center,
      radius: Math.round(center.distanceTo(corner))
    };
  }

  get() {
    return { ... this.value };
  }

  private static normalizeBounds(bounds: LatLngBounds) {
    return Leaflet.latLngBounds(
      BoundsChangeTracker.normalizeCoord(bounds.getNorthEast()),
      BoundsChangeTracker.normalizeCoord(bounds.getSouthWest()));
  }

  private static normalizeCoord(coord: LatLng) {
    return Leaflet.latLng(
      BoundsChangeTracker.round(coord.lat),
      BoundsChangeTracker.round(coord.lng));
  }

  refresh() {
    const from = this.value.center.toBounds(this.value.radius);
    const to = BoundsChangeTracker.normalizeBounds(this.map.getBounds());

    if (from.contains(to) || from.equals(to)) {
      return false;
    }

    const e = BoundsChangeTracker.createEvent(to.pad(BoundsChangeTracker.PADDING));
    const changed = {
      center: !this.value.center.equals(e.center),
      radius: this.value.radius !== e.radius,
    };

    if ((!changed.center && !changed.radius)) {
      // NOTE: no change
      return false;
    }

    if (!changed.center && e.radius < this.value.radius) {
      // NOTE: no change (zoomed in)
      return false;
    }

    if (!changed.radius && (!changed.center || e.center.distanceTo(this.value.center) < (e.radius * BoundsChangeTracker.PADDING))) {
      // NOTE: no change (panned within threshold)
      return false;
    }

    this.value = e;
    return true;
  }

  private static round(value: number) {
    return Math.round(value * 1e2) / 1e2;
  }
}

type MapOptions = BaseMapOptions & {
  enableLocate?: boolean;
  keepBuffer?: number;
}

type MapEvents = {
  invokeMethodAsync<K extends keyof MapEventMap>(methodName: K, arg?: MapEventMap[K]): Promise<void>;
}

type MapEventMap = {
  'OnBoundsChanged': OnBoundsChangedEvent;
  'OnReady': OnReadyEvent;
}

type MarkerEvents = {
  invokeMethodAsync<K extends keyof MarkerEventMap>(methodName: K, arg?: MarkerEventMap[K]): Promise<void>;
}

type MarkerEventMap = {
  'OnPopupClosed': void;
  'OnPopupOpen': void;
}

type MarkerOptions = {
  autoPopup?: boolean;
  closeButton?: boolean;
  position: LatLngExpression;
  style?: MarkerStyle;
  title?: string;
}

enum MarkerStyle {
  Default,
  Custom,
}

type OnBoundsChangedEvent = {
  center: LatLng;
  radius: number;
}

type OnReadyEvent = {
  center: LatLng;
  radius: number;
}