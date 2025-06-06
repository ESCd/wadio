import Leaflet from 'leaflet';

Leaflet.Icon.Default.imagePath = '/Interop/map/';
export function createMap(element: HTMLElement, coordinate: Coordinates) {
  const map = Leaflet.map(element, {
    center: [coordinate.latitude!, coordinate.longitude!],
    maxZoom: 15,
    minZoom: 10,
    zoom: 15,
  });

  Leaflet.marker([coordinate.latitude!, coordinate.longitude!]).addTo(map);
  Leaflet.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 15,
    attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
  }).addTo(map);

  return {
    dispose() {
      map.off();
      map.remove();
    },

    refresh() {
      map.invalidateSize();
    }
  };
};

type Coordinates = {
  latitude: number;
  longitude: number;
};